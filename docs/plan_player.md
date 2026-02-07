# ZombieBox Phase 2: Professional Netcode Implementation Plan

## Executive Summary

This plan implements the "Trinity of Multiplayer" (Client-Side Prediction, Entity Interpolation, Lag Compensation) in 5 incremental phases. Each phase is independently testable and committable, ensuring the codebase remains functional throughout implementation.

**Timeline**: 5 phases, ~10-15 commits, estimated 15-20 hours
**Current State**: Phase 1 complete (server-authoritative, basic sync working)
**Target State**: Phase 2 complete (responsive input, smooth motion, fair shots)

---

## Critical Issues Identified (Must Fix First)

### Issue 1: PlayerInput Timing ⚠️ HIGH PRIORITY
**File**: `/scripts/Components/PlayerInput.cs:35`
**Problem**: Uses `_Process` (variable FPS) instead of `_PhysicsProcess` (60 FPS fixed)
**Impact**: Input sampling inconsistency between clients (PC rápido lee 2x por tick, PC lento 0.5x)
**Fix**: Change one line: `public override void _PhysicsProcess(double delta)`

### Issue 2: Visual-Physics Coupling 🔴 CRITICAL BLOCKER
**File**: `/scenes/entities/player/player.tscn`
**Problem**: Sprite2D is direct child of CharacterBody2D at (0,0) offset - cannot move independently
**Impact**: BLOCKS interpolation (cannot have visual position ≠ physics position)
**Fix**: Restructure scene to add VisualRoot intermediate node

### Issue 3: Velocity Not Synchronized ⚠️ MEDIUM
**File**: `/scenes/entities/player/player_state_server_to_all.tres`
**Problem**: Velocity property not in replication config (only position, rotation, Health, etc.)
**Impact**: Clients cannot extrapolate between updates, makes interpolation harder
**Fix**: Add `.:Velocity` to sync config

### Issue 4: Movement Logic Duplication ⚠️ MEDIUM
**Files**: `/scripts/Components/PlayerController.cs`, `/scripts/Components/ClientPredictor.cs` (future)
**Problem**: Physics calculation only exists in server's PlayerController
**Impact**: Client prediction needs to duplicate this logic - if they diverge, reconciliation every frame
**Fix**: Extract movement to `MovementUtils.ApplyMovement()` shared static method

### Issue 5: Walk Bob Animation Coupling 🟢 LOW (AWARE)
**File**: `/scripts/Components/PlayerVisuals.cs`
**Problem**: Bob animation reads `_input.MoveVector` directly - coupled to input state
**Impact**: If client prediction diverges from server, bob animation may desync slightly
**Fix**: Accept as minor issue for Phase 2 (visual only, doesn't affect gameplay). Future: base bob on velocity instead of input.

### Issue 6: Machine Gun Fire Timing ⚠️ MEDIUM (AWARE)
**File**: `/scripts/Components/PlayerController.cs`
**Problem**: Fire rate controlled by server-side timer only
**Impact**: Client sees shots after network round-trip (~50-100ms input lag for machine gun)
**Fix**: Accept for Phase 2 (pistol uses RPC which is fine). Future: client prediction for continuous fire.

---

## Implementation Phases

### Phase 0: Critical Fixes (Foundation Repair)
**Goal**: Fix blocking architectural issues

#### Commit 0.1: Fix PlayerInput Timing
- **Files**: `/scripts/Components/PlayerInput.cs`
- **Change**: Line 35: `_Process` → `_PhysicsProcess`
- **Test**: Run multiplayer, verify no input jitter
- **Time**: 5 minutes

#### Commit 0.2: Restructure Player Scene
- **Files**: `/scenes/entities/player/player.tscn`, `/scripts/Components/PlayerVisuals.cs`
- **Changes**:
  ```
  Player (CharacterBody2D)
  ├── VisualRoot (Node2D) ← NEW
  │   └── Sprite2D ← MOVED HERE
  ├── CollisionShape2D
  ├── PlayerInput
  └── ...
  ```
- **Test**: Players render correctly, walk bob still works
- **Time**: 1 hour

#### Commit 0.3: Add Velocity to Sync
- **Files**: `/scenes/entities/player/player_state_server_to_all.tres`
- **Change**: Add property `.:Velocity` with Server authority, Always mode
- **Test**: Remote clients see velocity values updating
- **Time**: 10 minutes

---

### Phase 1: Tick Foundation
**Goal**: Establish synchronized tick counter

#### Commit 1.1: Create TickManager
- **Files Created**: `/scripts/Core/TickManager.cs`
- **Files Modified**: `/scenes/systems/GameSession.tscn`
- **Implementation**:
  - TickManager singleton with ServerTick and ClientTick properties
  - MultiplayerSynchronizer to sync ServerTick
  - Add under GameSession/Systems/Managers/
- **Test**: Server tick increments at 60/sec, clients see it with 3-10 tick lag
- **Time**: 1 hour

#### Commit 1.2: Add Tick Stamps
- **Files Modified**:
  - `/scripts/Components/PlayerInput.cs` (add InputTick)
  - `/scripts/Entities/Player.cs` (add LastProcessedTick)
  - `/scripts/Components/PlayerController.cs` (set LastProcessedTick after processing)
  - `/scenes/entities/player/player_input_client_to_server.tres` (sync InputTick)
  - `/scenes/entities/player/player_state_server_to_all.tres` (sync LastProcessedTick)
- **Test**: Client sees server processed their tick correctly
- **Time**: 1 hour

---

### Phase 2: Client-Side Prediction
**Goal**: Client predicts own movement, reconciles with server

#### Commit 2.1: Create Utility Classes
- **Files Created**:
  - `/scripts/Core/StateSnapshot.cs` (data structures)
  - `/scripts/Core/CircularBuffer.cs` (generic buffer)
- **Test**: Unit test CircularBuffer Add/Get operations
- **Time**: 1 hour

#### Commit 2.2-2.3: ClientPredictor Component
- **Files Created**: `/scripts/Components/ClientPredictor.cs`
- **Files Modified**: `/scenes/entities/player/player.tscn` (add node)
- **Implementation**:
  - Store input history (120 ticks)
  - Store state history (120 ticks)
  - Predict movement using same logic as server
  - Compare with server position when it arrives
  - Reconcile if error > 0.5 units (reset + re-simulate)
- **Test**: Movement feels instant, reconciliation rare (<10/min)
- **Time**: 3 hours

#### Commit 2.4: Extract Shared Movement Logic
- **Files Created**: `/scripts/Core/MovementUtils.cs`
- **Files Modified**:
  - `/scripts/Components/PlayerController.cs`
  - `/scripts/Components/ClientPredictor.cs`
- **Change**: Both use `MovementUtils.ApplyMovement()` static method
- **Test**: Server and client physics identical, prediction accurate
- **Time**: 30 minutes

---

### Phase 3: Entity Interpolation
**Goal**: Remote players render smoothly between updates

#### Commit 3.1-3.2: RemoteInterpolator Component
- **Files Created**: `/scripts/Components/RemoteInterpolator.cs`
- **Files Modified**: `/scenes/entities/player/player.tscn` (add node)
- **Implementation**:
  - Buffer last 30 server states (0.5 seconds)
  - Render 100ms in past (6 ticks behind)
  - Find two states surrounding target tick
  - Interpolate position/rotation
  - Apply to VisualRoot (not CharacterBody2D)
- **Test**: Remote players glide smoothly, no jitter
- **Time**: 2 hours

#### Commit 3.3: Add Extrapolation Fallback
- **Files Modified**: `/scripts/Components/RemoteInterpolator.cs`
- **Change**: If only 1 state, extrapolate using velocity
- **Test**: Handles edge cases (reconnect, initial spawn)
- **Time**: 30 minutes

---

### Phase 4: Lag Compensation
**Goal**: Server rewinds time to validate shots

#### Commit 4.1: LagCompensator Skeleton
- **Files Created**: `/scripts/Core/LagCompensator.cs`
- **Files Modified**: `/scenes/systems/GameSession.tscn` (add under Managers)
- **Implementation**: Skeleton with RewindAndRaycast() method
- **Time**: 1 hour

#### Commit 4.2: Entity Tracking Integration
- **Files Modified**:
  - `/scripts/Systems/SpawnSystem.cs` (register entities on spawn)
  - `/scripts/Core/LagCompensator.cs` (store entity references, track positions)
- **Implementation**: Store 200 ticks of position history per entity
- **Test**: Server tracks all spawned players/enemies
- **Time**: 1 hour

#### Commit 4.3-4.4: Rewind-Raycast-Restore + Shooting Integration
- **Files Modified**:
  - `/scripts/Core/LagCompensator.cs` (complete RewindAndRaycast)
  - `/scripts/Entities/Player.cs` (modify shooting to include tick)
- **Implementation**:
  - Client sends shoot request with InputTick
  - Server rewinds entities to that tick
  - Performs raycast in rewound state
  - Restores entities to current
  - Applies damage if hit
- **Test**: Shots hit moving targets with 150ms lag, accuracy >80%
- **Time**: 2 hours

---

### Phase 5: Polish and Testing
**Goal**: Debug tools and performance validation

#### Commit 5.1-5.2: Debug Overlay + Stats
- **Files Created**: `/scripts/Debug/NetworkDebugOverlay.cs`
- **Files Modified**:
  - `/scripts/Components/ClientPredictor.cs` (add stats)
  - `/scenes/systems/GameSession.tscn` (add overlay)
- **Features**:
  - Display ServerTick, ClientTick, tick diff
  - Show reconciliation count and average error
  - Toggle with F1
- **Time**: 1 hour

#### Commit 5.3: Performance Testing
- **Test Plan**:
  1. Baseline (no netcode): 50 enemies, measure FPS
  2. Client prediction: Verify <16ms input latency
  3. Interpolation: Verify smooth at 60 FPS
  4. Lag comp: 90%+ accuracy with 150ms lag
  5. Stress test: 10 players + 100 enemies, <10% overhead
- **Deliverable**: `/docs/PHASE2_PERFORMANCE_REPORT.md`
- **Time**: 2 hours

---

## Critical Files to Modify

| Priority | File | Changes |
|----------|------|---------|
| 🔴 Critical | `/scripts/Components/PlayerInput.cs` | Fix timing (_Process → _PhysicsProcess) |
| 🔴 Critical | `/scenes/entities/player/player.tscn` | Add VisualRoot node structure |
| 🔴 Critical | `/scripts/Components/ClientPredictor.cs` | NEW: Prediction + reconciliation |
| 🔴 Critical | `/scripts/Components/RemoteInterpolator.cs` | NEW: Smooth interpolation |
| 🔴 Critical | `/scripts/Core/LagCompensator.cs` | NEW: Server-side rewind |
| 🟡 High | `/scripts/Core/TickManager.cs` | NEW: Tick synchronization |
| 🟡 High | `/scripts/Core/CircularBuffer.cs` | NEW: History buffers |
| 🟡 High | `/scripts/Core/MovementUtils.cs` | NEW: Shared physics logic |
| 🟢 Medium | `/scripts/Entities/Player.cs` | Add LastProcessedTick property |
| 🟢 Medium | `/scenes/entities/player/*.tres` | Update sync configs |

---

## Verification & Testing

### Per-Phase Tests

**Phase 0**: Run multiplayer, move players, verify no visual regression
**Phase 1**: Check tick counters sync, verify tick stamps in network traffic
**Phase 2**: Movement feels instant, reconciliation <10/min with normal lag
**Phase 3**: Remote players glide smoothly, no jitter or stuttering
**Phase 4**: Shots hit moving targets with 150ms lag, >80% accuracy
**Phase 5**: Performance <10% overhead vs Phase 1, all systems stable

### End-to-End Test

1. Launch server + 3 clients
2. Add 150ms artificial network lag
3. All clients move continuously in random patterns
4. All clients shoot at each other
5. Run for 5 minutes
6. **Expected Results**:
   - Movement feels instant on own client (prediction)
   - Remote players move smoothly (interpolation)
   - Shots register accurately (lag compensation)
   - FPS remains stable (>50 FPS)
   - Reconciliation rare (<5% of frames)

---

## Risk Mitigation

### Risk 1: Reconciliation Thrashing
**Symptom**: Client reconciles every frame
**Cause**: Movement logic mismatch or floating-point errors
**Fix**: Use shared MovementUtils, increase threshold to 1.0 unit

### Risk 2: Interpolation Stuttering
**Symptom**: Remote players jitter
**Cause**: Buffer underflow or packet loss
**Fix**: Increase delay to 150ms, add extrapolation fallback

### Risk 3: Lag Comp False Positives
**Symptom**: Shots hit behind cover
**Cause**: Rewind too far or interpolation offset
**Fix**: Reduce max rewind to 150ms, log all rewinds

### Risk 4: Performance Degradation
**Symptom**: FPS drops
**Cause**: Reconciliation or rewind overhead
**Fix**: Reduce buffer sizes, optimize with spatial partitioning

---

## Rollback Strategy

If critical bugs emerge:

1. **Immediate**: Remove new components from player.tscn, restart
2. **Feature Flags**: Add enable/disable toggles in project settings
3. **Git Revert**: Each phase is a commit, can revert individually
4. **Keep Foundation**: TickManager and tick stamps are safe to keep

---

## Success Criteria

- ✅ Input latency: <16ms (instant feel)
- ✅ Remote smoothness: 60 FPS with no jitter
- ✅ Shot accuracy: >80% with 150ms lag
- ✅ Reconciliation rate: <5% of frames
- ✅ Performance overhead: <10% vs Phase 1
- ✅ No visual regressions from Phase 1

---

## Post-Implementation

1. Update `/docs/ARCHITECTURE.md` with Phase 2 components
2. Create `/docs/PHASE2_IMPLEMENTATION_NOTES.md` (lessons learned)
3. Write `/docs/PHASE2_PERFORMANCE_REPORT.md` (test results)
4. Update README.md with new testing procedures

---

**Ready to implement?** This plan provides a clear, incremental path with safety nets at every step.
