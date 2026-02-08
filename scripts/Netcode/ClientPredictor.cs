using Godot;
using ZombieBox.Netcode;

/// <summary>
/// ClientPredictor - Component for client-side prediction.
///
/// ONLY runs on clients, ONLY for the local player (IsMultiplayerAuthority).
/// Remote players use RemoteInterpolator (Phase 3).
///
/// Responsibilities:
/// 1. Predict own movement immediately (0ms input lag)
/// 2. Store input/state history (last 120 ticks)
/// 3. Reconcile when server confirmation arrives
/// 4. Re-simulate if prediction error detected
///
/// Flow each frame:
/// 1. TryReconcile() - Fix past errors first
/// 2. StoreInput() - Save current input
/// 3. PredictMovement() - Apply physics immediately
/// 4. StorePredictedState() - Save predicted state
/// </summary>
public partial class ClientPredictor : Node
{
	// ========================================
	// CONFIGURATION
	// ========================================

	/// <summary>
	/// Error threshold for reconciliation (in world units).
	/// If prediction error > threshold, reconcile.
	/// Decision 1: 0.5 units (balance between precision and smoothness)
	/// </summary>
	private const float RECONCILIATION_THRESHOLD = 0.5f;

	/// <summary>
	/// History buffer size (ticks to remember).
	/// 120 ticks = 2 seconds at 60Hz.
	/// Enough to handle up to 2 seconds of network delay.
	/// </summary>
	private const int HISTORY_SIZE = 120;

	// ========================================
	// DEPENDENCIES
	// ========================================

	private Player _player;
	private PlayerInput _input;
	private TickManager _tickManager;

	// ========================================
	// STATE
	// ========================================

	/// <summary>
	/// Input history: stores player inputs by tick.
	/// Used for re-simulation during reconciliation.
	/// </summary>
	private CircularBuffer<InputSnapshot> _inputHistory;

	/// <summary>
	/// State history: stores predicted states by tick.
	/// Used to compare with server confirmation.
	/// </summary>
	private CircularBuffer<StateSnapshot> _stateHistory;

	/// <summary>
	/// Last tick we reconciled.
	/// Prevents re-reconciling the same tick multiple times.
	/// </summary>
	private uint _lastReconciledTick = 0;

	// ========================================
	// STATS (for debugging)
	// ========================================

	private int _reconciliationCount = 0;
	private float _totalError = 0f;

	// ========================================
	// INITIALIZATION
	// ========================================

	/// <summary>
	/// Initialize with dependency injection (preferred).
	/// Activates physics processing once TickManager is available.
	/// </summary>
	public void Initialize(TickManager tickManager)
	{
		_tickManager = tickManager;
		SetPhysicsProcess(true); // Enable processing now that we have TickManager
		GD.Print($"ClientPredictor: TickManager injected, physics processing enabled");
	}

	public override void _Ready()
	{
		// Only run on clients (not server)
		if (NetworkUtils.IsServer())
		{
			QueueFree();
			return;
		}

		_player = GetParent<Player>();
		_input = _player.GetNode<PlayerInput>("PlayerInput");

		// Disable physics processing until TickManager is injected
		SetPhysicsProcess(false);

		_inputHistory = new CircularBuffer<InputSnapshot>(HISTORY_SIZE);
		_stateHistory = new CircularBuffer<StateSnapshot>(HISTORY_SIZE);

		// Safety check: warn if injection doesn't happen within reasonable time
		GetTree().CreateTimer(1.0).Timeout += () =>
		{
			if (_tickManager == null)
			{
				GD.PrintErr($"ClientPredictor: FATAL - TickManager never injected for player {_player.Name}!");
			}
		};

		GD.Print($"ClientPredictor: Initialized for player {_player.Name}, waiting for TickManager");
	}

	// ========================================
	// MAIN LOOP
	// ========================================

	public override void _PhysicsProcess(double delta)
	{
		// Only predict for local player (not remote players)
		// Remote players will use RemoteInterpolator (Phase 3)
		if (!IsMultiplayerAuthority())
			return;

		uint currentTick = _tickManager.GetClientTick();

		// Step 1: Reconcile FIRST (fix past errors before predicting current frame)
		TryReconcile();

		// Step 2: Store current input
		StoreInput(currentTick);

		// Step 3: Predict movement (apply physics immediately - 0ms lag!)
		PredictMovement();

		// Step 4: Store predicted state
		StorePredictedState(currentTick);
	}

	// ========================================
	// STEP 1: RECONCILIATION
	// ========================================

	/// <summary>
	/// Try to reconcile with server.
	/// Decision 2: Only reconcile when there's actual error (optimized).
	/// </summary>
	private void TryReconcile()
	{
		uint serverProcessedTick = _player.LastProcessedTick;

		// No confirmation from server yet
		if (serverProcessedTick == 0)
			return;

		// Already reconciled this tick
		if (serverProcessedTick <= _lastReconciledTick)
			return;

		// Try to get our predicted state for that tick
		if (!_stateHistory.TryGet(serverProcessedTick, out StateSnapshot predictedState))
		{
			// Tick too old (not in buffer anymore), skip
			_lastReconciledTick = serverProcessedTick;
			return;
		}

		// Compare predicted position vs server position
		Vector2 serverPosition = _player.Position; // Replicated from server
		float error = predictedState.Position.DistanceTo(serverPosition);

		// Decision 2: Only reconcile if error > threshold
		if (error > RECONCILIATION_THRESHOLD)
		{
			// ERROR DETECTED - Need to reconcile!
			GD.Print($"[RECONCILIATION] Tick {serverProcessedTick}: Error={error:F2} units (threshold={RECONCILIATION_THRESHOLD})");

			// Stats
			_reconciliationCount++;
			_totalError += error;

			// RECONCILIATION PROCESS:
			// 1. Reset to server's authoritative state
			// 2. Re-simulate all ticks from serverProcessedTick+1 to current

			Reconcile(serverProcessedTick, serverPosition);
		}

		// Mark this tick as reconciled (even if no error, don't check again)
		_lastReconciledTick = serverProcessedTick;
	}

	/// <summary>
	/// Perform reconciliation: reset to server state and re-simulate.
	/// Decision 3: Teleport instant (no smoothing).
	/// </summary>
	private void Reconcile(uint serverTick, Vector2 serverPosition)
	{
		uint currentTick = _tickManager.GetClientTick();

		// Step 1: Reset to server's authoritative state
		_player.Position = serverPosition;
		_player.Velocity = _player.SyncedVelocity; // Server's velocity

		GD.Print($"[RECONCILIATION] Reset to server state: Pos={serverPosition}");

		// Step 2: Re-simulate all ticks from serverTick+1 to currentTick-1
		// (currentTick will be predicted normally in this frame's PredictMovement)
		for (uint tick = serverTick + 1; tick < currentTick; tick++)
		{
			// Get input for this tick
			if (_inputHistory.TryGet(tick, out InputSnapshot input))
			{
				// Re-apply movement with that input
				ApplyMovement(input.MoveVector, input.AimDirection);

				// Update state history with re-simulated state
				var reSimulatedState = new StateSnapshot(
					tick,
					_player.Position,
					_player.Velocity,
					_player.Rotation
				);
				_stateHistory.Add(tick, reSimulatedState);
			}
		}

		GD.Print($"[RECONCILIATION] Re-simulated {currentTick - serverTick - 1} ticks. New position: {_player.Position}");
	}

	// ========================================
	// STEP 2: STORE INPUT
	// ========================================

	/// <summary>
	/// Store current input in history.
	/// </summary>
	private void StoreInput(uint tick)
	{
		var inputSnapshot = new InputSnapshot(
			tick,
			_input.MoveVector,
			_input.AimDirection
		);

		_inputHistory.Add(tick, inputSnapshot);
	}

	// ========================================
	// STEP 3: PREDICT MOVEMENT
	// ========================================

	/// <summary>
	/// Predict movement for current tick.
	/// Uses same physics as server (will be extracted to MovementUtils in Commit 2.4).
	/// Decision 5: Use MoveAndSlide (detect collisions during prediction).
	/// </summary>
	private void PredictMovement()
	{
		ApplyMovement(_input.MoveVector, _input.AimDirection);
	}

	/// <summary>
	/// Apply movement physics using shared logic.
	/// Uses MovementUtils to ensure identical physics with server.
	/// </summary>
	private void ApplyMovement(Vector2 moveVector, Vector2 aimDirection)
	{
		MovementUtils.ApplyMovement(
			_player,
			moveVector,
			aimDirection,
			_player.Speed
		);
	}

	// ========================================
	// STEP 4: STORE PREDICTED STATE
	// ========================================

	/// <summary>
	/// Store predicted state in history.
	/// </summary>
	private void StorePredictedState(uint tick)
	{
		var stateSnapshot = new StateSnapshot(
			tick,
			_player.Position,
			_player.Velocity,
			_player.Rotation
		);

		_stateHistory.Add(tick, stateSnapshot);
	}

	// ========================================
	// DEBUG
	// ========================================

	/// <summary>
	/// Print reconciliation stats (for debugging).
	/// </summary>
	public void PrintStats()
	{
		float avgError = _reconciliationCount > 0 ? _totalError / _reconciliationCount : 0;
		GD.Print($"[ClientPredictor Stats] Reconciliations: {_reconciliationCount}, Avg Error: {avgError:F2} units");
	}
}
