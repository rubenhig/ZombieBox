# Plan de Proyecto: Juego Endless 2D Multijugador (Top-Down, Godot) 📋

## Objetivo y Alcance del Proyecto 🎯

Este proyecto consiste en desarrollar desde cero un videojuego top-down (vista cenital) de tipo endless shooter con soporte multijugador en línea. En su versión inicial, el juego ofrecerá un modo Endless cooperativo (o competitivo) donde uno o varios jugadores sobreviven a oleadas infinitas de enemigos. Dado que se comienza de cero, aplicaremos buenas prácticas iniciales en arquitectura y código para facilitar futuras expansiones. Los principales objetivos y características de esta primera versión son:

- Jugabilidad base: Controlar un personaje en 2D con vista cenital, moverse en un mapa delimitado y disparar armas contra enemigos ilimitados.
- Modo Endless: Generación continua de enemigos (ej. zombies) en oleadas crecientes, de forma que el juego termina solo cuando los jugadores son derrotados.
- Multijugador online: Desde el principio, la arquitectura soportará multijugador cliente-servidor real, permitiendo que varios jugadores se conecten a una partida y vean sus personajes en la misma arena. Se planteará un modelo de servidor autoritativo para mantener la consistencia del juego entre clientes[1].
- Armas básicas: Implementación de al menos dos armas por jugador (por ejemplo, pistola y metralleta), con diferentes cadencias de tiro.
- Enemigos básicos: Enemigos sencillos (e.g. zombies) con IA básica (perseguir al jugador más cercano) y que atacan al jugador. Serán eliminados al recibir disparos.
- Soporte IA en desarrollo: (Nota: Aunque el desarrollo contará con ayuda de herramientas de IA generativa, el plan se centra en la estructura y tareas. La integración con IA no afecta a la funcionalidad final, pero la claridad arquitectónica ayudará a usar dichas herramientas eficazmente.)

Alcance limitado: Esta versión inicial se enfocará en la funcionalidad básica descrita. Aspectos como menús avanzados, múltiples modos de juego, progresión o matchmaking complejo quedarán fuera de esta iteración. Sin embargo, la arquitectura se diseñará con miras a futuros modos y expansiones, manteniendo el código modular y reutilizable.

## Tecnologías, Lenguaje y Herramientas 🛠

El proyecto se desarrollará con el motor Godot Engine 4.x (se recomienda la versión estable más reciente). Para programar la lógica del juego se considera usar C# (Mono) en Godot, ya que el usuario está cómodo trabajando en VS Code y desea aprovechar la robustez de este lenguaje. Godot soporta C# nativamente en su versión Mono, lo que permite utilizar herramientas de desarrollo .NET (por ejemplo, OmniSharp para VSCode) y acceder a librerías .NET si fuese necesario[2]. Algunos puntos a justificar:

- C# vs GDScript: GDScript es el lenguaje integrado de Godot, muy utilizado por la comunidad y fácil de prototipar. Sin embargo, C# ofrece tipado estático, mayor rendimiento en cálculos intensivos y mejor integración con entornos externos (como VSCode)[3]. Dado que buscamos buenas prácticas desde el inicio y el usuario tiene preferencia por VSCode, optaremos por C#. Aun así, es bueno saber que Godot 4 cuenta con una extensión oficial para VSCode que permite editar GDScript externamente[4], por lo que GDScript también habría sido viable. En resumen, escogeremos C# para beneficiarnos de un entorno de desarrollo maduro y un lenguaje familiar, manteniendo la posibilidad de interoperar con GDScript si hiciera falta.
- Godot (Motor): Usaremos Godot 4 con soporte Mono (C#). Es importante crear el proyecto con la plantilla de C# habilitada. Esto generará una solución .NET y nos permitirá escribir scripts C# (.cs) vinculados a los nodos de Godot.
- IDE/Editor: Visual Studio Code será el entorno principal para editar código. Se configurará con el plugin de C# y/o el de Godot para VSCode, de forma que podamos alternar entre el editor Godot (para diseño de escenas) y VSCode (para código) cómodamente.
- Control de versiones: Se recomienda iniciar el repositorio Git desde el principio, incluyendo en .gitignore las carpetas generadas por Godot (como .import/). Mantener control de versiones permitirá seguir el progreso de cada tarea y facilita la colaboración (aunque sea con IA).
- Plataforma de destino: Inicialmente PC (Windows/Linux/Mac) para pruebas locales. La arquitectura online se basará en conexiones directas (posiblemente en LAN al inicio); para jugar a través de Internet habría que exponer puertos/usar NAT, dado que Godot usa UDP en su API de red[5], pero esto es un detalle de despliegue más que de desarrollo.

## Arquitectura del Juego 🏗️

Dado el carácter multijugador y escalable del proyecto, definiremos una arquitectura modular, clara y orientada a escenas (como es idiomático en Godot). Aplicaremos principios de organización recomendados: usar un script controlador por escena, escenas autocontenidas y nombres consistentes[6]. Asimismo, estructuraremos el proyecto en carpetas lógicas para recursos, separando escenas, scripts y assets según mejores prácticas[7][8]. A continuación, se detallan la estructura de archivos/carpetas propuesta, la composición de escenas y los componentes clave de la arquitectura:

- Estructura de carpetas: En el directorio del proyecto Godot, crearemos las siguientes carpetas principales:
  - scenes/ – Contendrá las escenas principales del juego (cada escena en su propio fichero .tscn). Ej: scenes/main.tscn, scenes/player.tscn, scenes/enemy.tscn, etc.
  - scripts/ (ó src/) – Contendrá los scripts fuente C# del juego, organizados por funcionalidad. Por ejemplo: scripts/Player.cs, scripts/Enemy.cs, scripts/NetworkManager.cs, etc. (En C#, por conveniencia, los nombres de archivo podrán ser PascalCase para coincidir con los nombres de clase).
  - assets/ – Recursos gráficos, sonidos, y otros assets. Podríamos subdividir en assets/sprites/, assets/sounds/, etc., según convenga.
  - (Opcional) addons/ – Carpeta reservada para plugins de Godot, en caso de usarse en el futuro.

Esta organización busca separar claramente lógica (scripts), datos (assets) y presentación (escenas)[7]. Usaremos snake_case para nombres de archivos y carpetas (p.ej. player.tscn, enemy.png) siguiendo la guía oficial, a fin de evitar problemas de mayúsculas entre sistemas operativos[8]. Las clases C# se nombrarán en PascalCase y se ubicarán preferiblemente en scripts/ para fácil navegación del código[9].

- Escenas y nodos principales: Cada entidad importante del juego será una escena independiente para fomentar la modularidad y la reutilización. A continuación, se describe un esquema de escenas y su jerarquía de nodos relevantes en la versión inicial:

```text
Main.tscn (Escena principal, root tipo Node o Node2D)
├── NetworkManager (Node) - Nodo para gestión de red (puede ser un Autoload singleton)
├── Game (Node2D) - Nodo contenedor de la lógica del juego (mundo)
│   ├── Player (CharacterBody2D) - Instancia del jugador local (prefab de player.tscn)
│   │   ├── Sprite (Sprite2D) - Representación visual del jugador
│   │   └── CollisionShape2D (colisión del jugador)
│   ├── (Player remoto 2, 3, ... adicionales si hay más jugadores, instanciados dinámicamente)
│   ├── Enemies (Node2D) - Nodo contenedor para enemigos activos
│   │   └── *Enemy (CharacterBody2D)* - Instancias enemigas (prefab de enemy.tscn, uno por enemigo)
│   │        ├── Sprite (Sprite2D) - Visual del enemigo (zombie)
│   │        └── CollisionShape2D 
│   ├── WaveManager (Node) - Controlador de oleadas de enemigos
│   │   └── SpawnPoint1, SpawnPoint2, ... (Position2D/Marker2D) - Puntos de aparición predefinidos
│   └── (Otros nodos del mundo, ej: TileMap del escenario, obstáculos, etc. si aplica)
└── UI (CanvasLayer / Control) - Interfaz de usuario superpuesta
    ├── HUD (Control/Nodo UI) - Ej. Barra de vida, contador de puntuación/oleada
    └── (Otros elementos UI, ej: texto "Game Over", menú pausa en versiones futuras)
```

Descripción: - La escena Main servirá como punto de entrada del juego (configurada como Main Scene en el proyecto). Su función es cargar/contener los demás nodos principales y persistir durante la ejecución. Podría ser simplemente un Node vacío que actúa de orquestador. Dentro de Main, un nodo hijo Game (Node2D) contendrá la lógica de la partida en curso. - NetworkManager: será responsable de la configuración de red (iniciar servidor/cliente, manejar conexiones y desconexiones). Podemos implementarlo de dos formas: - Autoload Singleton: Definir NetworkManager.gd/.cs como AutoLoad (Singleton) para que exista globalmente. Esto facilita que esté accesible en cualquier escena y persista aunque se cambien escenas (en este proyecto, quizás no haremos cambio de escena tras iniciar la partida, pero es útil para un futuro menú -> juego). Como singleton, se iniciaría al arrancar la aplicación, escuchando conexiones si es servidor o intentando conectar si es cliente. - Nodo dentro de Main: Alternativamente, instanciar un nodo NetworkManager como hijo de Main (como en el esquema). Dado que Main no se recarga durante la partida, este nodo cumpliría casi la misma función que un autoload. En ambos casos, NetworkManager manejará la capa de comunicación (usando la API de alto nivel de Godot sobre ENet). - Player: es una escena (e.g. player.tscn) con root CharacterBody2D (nodo físico 2D adecuado para personajes controlables). Incluye un sprite (la imagen del personaje) y colisión (CollisionShape2D) como hijos. Su script (Player.cs) gestionará la entrada del usuario (movimiento, rotación hacia dirección de movimiento, disparo cuando se pulsa el botón correspondiente) y la instanciación de balas. En multijugador, habrá múltiples instancias de Player (una por jugador conectado). Cada instancia puede tener una propiedad identificadora (p.ej. un peer_id de red o un nombre). Solo el jugador local será controlado por la entrada local; los demás se moverán en base a datos recibidos del servidor. - Enemy: escena enemy.tscn con root CharacterBody2D (por ejemplo representando un zombie). Tiene sprite y colisión propios. Su script (Enemy.gd/cs) implementa comportamiento simple: buscar y perseguir al jugador más cercano (o al jugador 1 por simplicidad inicial). En el modo multijugador, los enemigos también serán controlados únicamente por el servidor (IA corre en el servidor) y sus posiciones se sincronizan a clientes. Todos los enemigos podrían pertenecer al grupo "enemigo" (o "zombie") para facilitar referencias globales (por ejemplo, hacer que las balas detecten si han chocado con un cuerpo en el grupo enemigo)[10]. - Bullet: escena bullet.tscn (Área2D) para las balas/proyectiles. Contendrá un CollisionShape2D (para colisiones) y posiblemente una pequeña imagen Sprite. Su script (Bullet.gd/cs) manejará el movimiento rectilíneo en la dirección disparada y detectará colisiones con enemigos. Al salir de la pantalla o impactar, se destruirá (llamando queue_free() para liberar). En red, las balas serán generadas por el servidor y replicadas a los clientes para evitar discrepancias (el servidor decide qué enemigos mueren). - WaveManager: Nodo (script WaveManager.gd/cs) encargado de la lógica Endless: es decir, de crear oleadas sucesivas de enemigos. Una estrategia simple: definir un número base de enemigos por oleada e incrementar este número en cada nueva ola. Por ejemplo, en la ola 1 spawnear 3 enemigos, ola 2 -> 6 enemigos, etc. Este nodo usará los SpawnPoint definidos (hijos marcadores en la escena) para posicionar aleatoriamente a los enemigos generados[11]. Tras spawnear una oleada, WaveManager puede esperar un intervalo (time_between_waves) antes de generar la siguiente[12], creando un ciclo infinito. En el servidor, WaveManager estará activo generando enemigos; en los clientes podría estar inactivo (ya que los enemigos llegarán vía red) o estar presente solo para estructura pero recibiendo las instancias replicadas desde el servidor. - UI (HUD): Aunque la interfaz de usuario será mínima al inicio, conviene planificar un CanvasLayer para elementos HUD. Por ejemplo, mostrar la vida del jugador, el número de enemigos eliminados o la oleada actual. Estos elementos se actualizarán en tiempo real (y pueden usar datos sincronizados: p.ej., la oleada actual la conoce el servidor y podría enviarla a clientes para mostrar). El UI puede incluir también indicaciones de controles o un simple menú de pausa. (Para la versión inicial, elementos sencillos: un Label para la puntuación o tiempo sobrevivido, etc.)

Este diseño de escenas permite mantener cada componente aislado y claro. Cada escena tiene su script controlador (p. ej. Player.cs controla a su jugador, Enemy.cs su IA, etc.), favoreciendo la encapsulación[13]. La comunicación entre escenas/nodos se hará mediante señales o métodos públicos bien definidos, evitando dependencias cruzadas fuertes. Por ejemplo, el Player puede emitir una señal "disparo realizado" que el servidor capte para crear la bala, o el Enemy puede emitir "enemigo_muerto" para actualizar la puntuación. Así mantenemos el código modular.

- Modelo de red cliente-servidor: Desde el principio adoptaremos un esquema servidor autoritativo. Esto significa que uno de los pares actuará como servidor central (posiblemente uno de los jugadores “hostea” la partida, o un servidor dedicado), y toda la lógica del juego se resolverá en ese servidor, mientras que los clientes simplemente envían sus entradas y reciben el estado resultante[14]. Esta elección de arquitectura garantiza que haya una única fuente de verdad para la simulación del juego, evitando discrepancias entre jugadores (por ejemplo, evitando que cada cliente “simule” cosas por su cuenta de manera no sincronizada). De hecho, se considera que un modelo autoritativo es prácticamente obligatorio para consistencia en multiplayer: “tener un servidor como autoridad no es opcional... siempre hay un servidor, incluso si corre en la máquina de un jugador”[1]. En la práctica:
  - El servidor manejará la física y la lógica: moverá a todos los personajes (incluidos los jugadores, aplicando las entradas de cada uno), calculará colisiones (disparos que impactan, enemigos que alcanzan a jugadores), generará enemigos y resolverá condiciones de victoria/derrota.
  - Los clientes enviarán sus acciones (ej: input de movimiento, comando de disparo) al servidor, y recibirán actualizaciones del mundo (posiciones de jugadores, aparición/eliminación de enemigos, etc.) para reflejarlas localmente. Los clientes renderean la información recibida, pero no toman decisiones que afecten al estado global sin aprobación del servidor.
  - Esta estructura suele implicar un ligero retraso de red, pero garantiza que “si funciona en el servidor, funciona igual para todos”[15][16]. Además, simplifica depuración: muchas inconsistencias se eliminan al no dividir la autoridad de los objetos del juego[17].
  - Godot facilita este modelo con su High-level Multiplayer API, que permite sincronizar nodos y RPCs de forma conveniente. Internamente emplea ENet sobre UDP para eficiencia en tiempo real[5]. Nosotros inicializaremos esta API creando un servidor en la instancia host (p. ej. ENetMultiplayerPeer.create_server(port, max_clients)) y conectando los clientes (create_client(ip, port))[18]. Una vez conectados, podremos usar RPCs anotados (con la etiqueta @rpc) para invocar métodos remotos en otros nodos[19]. Por ejemplo, el cliente podría llamar rpc_id(1, "mover_jugador", input_dir) para enviar su intención de movimiento al servidor (ID 1 es siempre el server) y el servidor ejecutaría la lógica actualizando la posición del jugador correspondiente.
  - Se prestará especial atención a la identidad de nodos en red: para que RPCs y sincronización funcionen, los paths de los nodos deben coincidir en cliente y servidor[20]. Es decir, por ejemplo todos los jugadores podrían instanciarse bajo un mismo padre con nombres consistentes (como "Player1", "Player2", etc. o usando force_readable_name al instanciar via código)[21]. Definiremos convenciones para esto (quizá renombrar el nodo root del Player a algo único según el peer).
  - Reparto de autoridad: Por defecto en Godot, el servidor es autoridad de todos los nodos (excepto que se especifique lo contrario). En este proyecto, dejaremos que el servidor sea dueño de la mayoría de nodos (jugadores, enemigos, balas). Los clientes tendrán autoridad solo de sus entradas. Esto encaja con la filosofía de "el servidor lo maneja todo"[22]. Implementaremos las funciones de movimiento, disparo, spawn de enemigos, etc., de forma que solo corran en el servidor, y los clientes hagan poco más que solicitar o reproducir resultados. Por ejemplo, en el script Player, podríamos hacer que su _PhysicsProcess solo afecte si es el servidor (if (Multiplayer.IsServer()) { ... mover lógica ... } en C#)[23].

Sincronización de estado: Para propagar el estado del juego a todos los clientes, usaremos una combinación de RPCs y sincronización automática:

- Variables críticas (posiciones, animaciones) se pueden sincronizar usando @rpc(sync="...") o utilizando nodos sincronizadores como MultiplayerSynchronizer en Godot 4. Sin embargo, dado que buscamos eficiencia y control manual (como recomienda cierta experiencia[24]), podríamos optar por enviar solo datos necesarios via RPC. Por ejemplo: el servidor emite un RPC a todos (rpc() broadcast) con la nueva posición de un jugador tras procesar la entrada, o con la información de que se ha instanciado un enemigo nuevo, etc.
- Las balas se pueden manejar de dos formas: (a) usar nodos MultiplayerSpawner/MultiplayerSynchronizer de Godot para replicarlas automáticamente, o (b) manualmente vía RPC. Una práctica aconsejada es no abusar de sincronización automática para todo, sino sincronizar sólo lo necesario[25]. Aquí podríamos hacer que cuando un jugador dispara, el cliente envía una solicitud al servidor, este crea la bala en su escena (autoritaria) y luego envía un RPC a todos los clientes indicándoles que creen la bala localmente en tal posición y dirección. Así garantizamos que todos ven la misma bala. Incluso podríamos simplificar: crear la bala en el servidor and marcarla como replicada para que Godot la cree automáticamente en clientes (usando MultiplayerSpawn con propiedad de réplica). Cualquiera de las dos sirve en esta escala.
- Para enemigos, el spawn lo hará el servidor (oleadas) y luego propagará la creación a los clientes. Los movimientos de los enemigos (persecución) ocurren en el servidor cada frame; para que los clientes lo vean, podemos: o bien sincronizar sus propiedades (posición, rotación) automáticamente con un MultiplayerSynchronizer, o enviar actualizaciones periódicas via RPC (p.ej. 10 veces por segundo). Dado que Godot 4 trae mejoras, se podría aprovechar la sincronización de Physics en HLAPI. En cualquier caso, la gestión de IA queda en servidor.
- En resumen, mantendremos la carga del procesamiento en el servidor y sincronizaremos solo lo imprescindible a los clientes (posición de objetos, animaciones, creación/eliminación de nodos)[26][25]. Esto reduce uso de red y evita conflictos de autoridad.

Persistencia de estado y futuras expansiones: La arquitectura planteada permitirá más adelante introducir nuevos modos de juego o características con mínima alteración del núcleo:

- Si quisiéramos añadir un modo diferente (ej. un modo arena PvP o cooperativo con objetivos), podríamos crear otra escena de GameMode separada y cargarla en Main según selección. Gracias a que el manejo de red y los componentes jugadores/enemigos están aislados, reutilizaríamos Player y NetworkManager en el nuevo modo, cambiando solo la lógica de spawn o reglas de victoria.
- La separación por escenas facilita que cada sistema (movimiento, combate, spawn, UI, networking) se pueda modificar o reemplazar individualmente. Por ejemplo, podríamos sustituir WaveManager por un sistema de spawning diferente sin afectar al resto, siempre que mantenga la interfaz esperada (spawn enemigos y notificarlos).
- Mantenemos la opción de correr un servidor dedicado (cabeza sin gráficos): Godot permite ejecutar en modo headless, y nuestra lógica al estar centralizada en servidor autoritativo, podría ejecutarse en una instancia sin mostrar gráficos, mientras los clientes se conectan[27][28]. Basta con asegurar que el código no asume existencia de interfaz gráfica en servidor (lo cual cumpliremos separando lógica de visual).
- En cuanto a buenas prácticas de código desde el arranque: se documentarán las funciones con comentarios, se usarán nombres descriptivos en español o inglés pero coherentes, y se evitará hardcodear valores donde no corresponda (se usarán constantes o variables exportadas para tunear desde el editor, p. ej. velocidad del jugador, cadencia de disparo, etc.). Además, aplicaremos convenciones del estilo de Godot (nomenclatura, evitar get_node excesivo prefiriendo variables onready o inyección de dependencias) para mantener el código limpio y fácil de entender.

## Funcionalidades Principales y Detalles de Implementación 🔍

A continuación, se describen con más detalle las funcionalidades básicas que debe tener el juego en esta versión, junto con consideraciones de implementación específicas para cada una:

- Movimiento del jugador: El jugador podrá desplazarse en las cuatro direcciones (arriba, abajo, izquierda, derecha) usando teclado (WASD o flechas, configurable). Se definirá un conjunto de Input Actions en Godot ("ui_up", "ui_down", etc., o acciones personalizadas "move_up", "move_left", etc.). El Personaje jugador (CharacterBody2D) leerá la entrada en cada frame (process or _physics_process) y aplicará una velocidad constante en la dirección indicada. Usaremos métodos físicos de Godot, p. ej. MoveAndSlide() o MoveAndCollide(), para gestionar el movimiento con colisiones. La rotación del sprite del jugador se orientará en la dirección de movimiento para mayor inmersión (como ya planificado, almacenando la última dirección para dirección de disparo)[29]. En caso de usar una animación (no imprescindible inicialmente), podríamos cambiar el sprite según dirección o reproducir animaciones de caminar.

Multijugador: El movimiento será sincronizado. El input del jugador local se envía al servidor (por RPC) cada frame o a intervalos cortos; el servidor actualiza la posición del personaje y luego difunde la nueva posición a todos los clientes. Para suavizar, se pueden emplear técnicas de interpolación en el cliente (interpolar entre posiciones recibidas) para evitar saltos, aunque en primera instancia se puede aceptar pequeños saltos dado el scope limitado. La velocidad de movimiento puede ser igual para todos los jugadores (valor constante, e.g. 200 px/s) para simplicidad.

Disparo y armas: Cada jugador tiene la capacidad de disparar proyectiles al presionar un botón (por ejemplo, clic de ratón o tecla de disparo). Para esta versión habrá 2 armas disponibles:

- Pistola: arma básica de tiro semiautomático. Dispara un proyectil por cada pulsación (no mantiene fuego continuo). Daño moderado.
- Metralleta (ametralladora): arma automática de mayor cadencia. Manteniendo pulsado el botón de disparo se generarán balas contínuamente (ráfaga). Cada bala puede hacer un poco menos de daño individualmente que la pistola, pero la cadencia compensa.

Implementación: Al apretar el botón de disparo (p.ej. action "shoot"), el script del Player creará una instancia de la escena Bullet. Usará un PackedScene precargado para la bala[30], la instanciará y la añadirá a la escena. Se le definirá la dirección inicial de la bala (por ejemplo, la dirección en la que mira o se mueve el jugador en ese momento) y una velocidad fija (propiedad del bullet, e.g. 400 px/s)[31]. La bala avanzará cada frame en esa dirección y detectará colisiones. Configuraremos la colisión de la bala para que detecte a enemigos (colisiones 2D en capas correspondientes, o mediante grupos: la bala puede detectar bodies en grupo "enemigo"[10]). Al colisionar con un enemigo, aplicará efectos: reduciremos vida del enemigo o simplemente destruiremos al instante (para esta versión, posiblemente un impacto = enemigo muerto, i.e. "one-shot kill"). Tras impactar o salir de la pantalla (usaremos un VisibleOnScreenNotifier para saber si salió del viewport[32]), la bala se destruirá para liberar memoria. - Multijugador: El disparo también seguirá el modelo autoritativo. En lugar de cada cliente crear sus balas localmente (lo que podría desincronizarse), haremos que el servidor cree las balas: - Cuando un jugador cliente pulsa disparar, enviará un mensaje al servidor (vía RPC) indicando "jugador X disparó con Y arma en tal dirección". - El servidor recibe esto, verifica (podría validar cadencia o munición si se complica en el futuro) y entonces instancia la bala en el mundo servidor. Acto seguido, notifica a los demás clientes para que reproduzcan esa bala. Podemos notificar vía RPC broadcast enviando posición inicial y dirección, para que cada cliente instancie localmente una bala con trayectoria idéntica. Alternativamente, aprovechando Godot, podemos marcar la bala como un nodo replicado: spawnearla en el servidor con rpc() para que Godot la cree automáticamente en clientes (o usando MultiplayerSpawner node). Cualquiera de las dos. Lo importante: la colisión oficial se calcula en servidor. Así, si la bala pega a un enemigo, el servidor decidirá eliminar al enemigo y luego enviará a clientes la orden de eliminar ese enemigo (o replicará la cola de liberación). - En el cliente local, podríamos opcionalmente generar un efecto inmediato de disparo (un destello de disparo o un sonido) para feedback instantáneo, aunque la bala en sí llegue tras confirmación del server. Para evitar sensación de lag, a veces se hace eso; pero en esta versión básica, podemos permitir que la bala aparezca con la ligera latencia del server, dado que el enfoque es cooperativo (no tan crítico como PvP). - Gestionaremos la cadencia: por ejemplo, para la metralleta, podemos implementar un temporizador que solo permita disparar cada X segundos. Este control debería estar también validado en servidor (no permitir más balas de las debidas por segundo). Variables como fire_rate o cooldown de arma se pueden definir en el script de Player o en un recurso separado si fuese complejo. Inicialmente, quizás harcodeamos: pistola (un tiro por click, sin autofire), metralleta (tiro automático, ej. 5 balas por segundo). - Cada bala podría pertenecer a un grupo "balas" si necesitáramos manejarlas en conjunto, aunque puede no hacer falta. Las balas se destruirán solas tras impacto o salida, con lo cual la limpieza está controlada.

- Enemigos y Oleadas (Endless): Los enemigos básicos serán zombies (u otra entidad genérica) que aparecen continuamente en oleadas incrementales:
  - Aparición: Utilizaremos el nodo WaveManager para orquestar spawns. Al iniciar la partida (o tras un breve delay de preparación), WaveManager creará un número de enemigos (ej: 3 * número_de_ola). Los puntos de spawn estarán predefinidos (p. ej., cuatro esquinas del mapa, representadas por nodos Position2D hijos de WaveManager)[33][34]. Para cada enemigo a spawnar, escogerá un spawn point aleatorio, instanciará la escena Enemy y la añadirá como hija del contenedor Enemies en la escena. Podemos usar call_deferred("add_child", enemy_instance) para evitar problemas si spawneamos dentro de un loop[34]. Tras crear una oleada, WaveManager puede esperar cierto tiempo (time_between_waves) usando un Timer o await de señal (como en GDScript async)[12] antes de lanzar la siguiente oleada incrementando el contador.
  - IA básica: Cada enemigo (Zombie) tendrá un comportamiento sencillo: en cada frame, calcula la dirección hacia el jugador más cercano y avanza en esa dirección a su velocidad definida[35]. Para la versión inicial, podríamos simplificar que persiga siempre al jugador 1 (host), o mejor, implementar que busque entre todos los jugadores conectados cuál está más cerca (iterando sobre nodos del grupo "players"). Si se desea simplificar aún más, se puede hacer que todos vayan al mismo jugador (hará el juego más difícil para ese jugador, pero más simple lógicamente). Una mejora podría ser repartir aleatoriamente objetivos entre jugadores.
  - Colisiones enemigo-jugador: Cuando un enemigo alcance a un jugador, definiremos qué ocurre: probablemente reducir vida del jugador. Podemos implementar que cada Player tenga una propiedad health. Si un zombie colisiona con un Player, le resta X puntos de vida y se destruye (suicida) o continúa hasta ser eliminado. Para esta versión, incluso podríamos hacer "muerte instantánea" al tocar, pero es mejor dar un margen. Por simplicidad, supongamos cada contacto quita 1 de vida y el jugador tiene, digamos, 3 de vida.
  - Escala infinita: El modo endless no tiene fin predeterminado, pero podríamos considerar alguna forma de puntaje para el jugador: e.g. contar cuántos enemigos eliminó o cuántas oleadas superó, y eso sería la puntuación final si muere. Ese puntaje se muestra en UI.

Multijugador: Toda la lógica de spawn e IA se realizará en el servidor exclusivamente. WaveManager correrá solo en servidor generando enemigos. Los clientes simplemente recibirán la creación de enemigos (ya sea automáticamente replicada o vía RPC instanciándolos). Para la IA, podemos optar por no ejecutar el código de seguimiento en clientes; en su lugar, los enemigos en clientes podrían ser marionetas que solo actualizan su posición según datos sincronizados. Sin embargo, una manera rápida en Godot es: instanciar los enemigos también con su script de IA en clientes pero envolver su movimiento con if (Multiplayer.IsServer()) como antes, para que en clientes no apliquen movimiento propio[23]. De ese modo, el mismo script corre en todos, pero solo hace efecto en servidor. Luego la posición se sincroniza, logrando que se muevan en pantalla de los clientes. Esto aprovecha el mismo código en ambos lados, manteniendo la autoridad en servidor.

- Colisiones y daño a jugadores igualmente se calcularán en servidor. Si un enemigo toca a un player, el servidor bajará la vida de ese player y quizá envíe una señal/RPC al cliente de ese player para actualizar su HUD de vida. Si la vida llega a 0, el servidor podría marcar a ese jugador como muerto (y en un coop, tal vez permitir que el otro siga; si todos mueren, fin de juego).
- Eliminación de enemigos: cuando un enemigo muere (por bala o por alguna otra causa), el servidor hará queue_free() en él. Godot replicará la destrucción al cliente automáticamente (ya que al ser nodo sincronizado, su eliminación se propaga). Podemos también enviar un RPC para aumentar puntuación en HUD.
- Colaboración entre jugadores: Dado que es cooperativo, todos los jugadores contribuyen a eliminar enemigos. Podríamos compartir una puntuación global (oleada más alta alcanzada, total enemigos abatidos entre todos) o individual (cada jugador kills). Esto se puede agregar en UI fácilmente, pero no es prioritario funcionalmente.

Interfaz de usuario (HUD): Incluiremos elementos de UI básicos:

- Indicador de vida: por jugador. Si solo mostramos en el cliente local, un simple icono/corazones o barra de vida del propio personaje. Si quisiéramos, para coop, podríamos mostrar las vidas de cada jugador (marcadas por su ID o color).
- Contador de oleada y/o enemigos: un texto indicando "Oleada: X" que se actualiza al inicio de cada oleada. Y/o "Enemigos eliminados: Y".
- Notificaciones: mensajes como "¡Nueva oleada!" o "Juego Terminado" cuando corresponda.
- Estos elementos estarán en un CanvasLayer para que permanezcan fijos en pantalla. Se actualizarán mediante señales o llamados desde la lógica. Ej: WaveManager puede emitir una señal wave_started(wave_number) que capture un script HUD para mostrar el número.
- Multijugador: Asegurar que las notificaciones que dependan de eventos del servidor lleguen a todos los clientes. Por ejemplo, el servidor puede mandar un RPC a todos con la nueva oleada comenzada para que cada uno actualice su HUD. O tener variables replicadas (pero es más directo solo mandar el dato necesario).

Un detalle: Conexión/Desconexión UI – En esta versión inicial, no se implementará un lobby gráfico elaborado. Para probar, se puede simplemente tener el juego iniciando inmediatamente. Sin embargo, podríamos ofrecer en Main una opción simple: un menú de texto para "Host" o "Join", donde:

- Si seleccionas Host, el juego crea un servidor y empieza la partida inmediatamente.
- Si seleccionas Join, solicita una IP (podría ser hardcode localhost para pruebas) y se conecta como cliente. Esto podría implementarse con un par de botones y una pequeña caja de texto, pero si es mucho, se puede hacer via configuración manual durante desarrollo (por ejemplo, arrancar una instancia con un flag "--server" y otras con "--client ip"). En cualquier caso, mencionamos la idea para la arquitectura.

Gestión de partidas (NetworkManager): Es una funcionalidad interna, pero importante resaltar:

- El NetworkManager deberá ofrecer métodos como start_server() y connect_to_server(ip) para iniciar la sesión de juego. Al iniciar servidor, definiremos el número máximo de jugadores (p.ej. 4). Cuando un cliente se conecte, el servidor lo detectará vía señal peer_connected[36]. Al ocurrir esto, el servidor debe instanciar un Player nuevo para ese cliente:
  - Podríamos tener una escena de Player diferenciada para local vs remotos, pero realmente puede ser la misma. Lo que cambia es quién lo controla. Podemos asignar al nodo Player un Network Role: Godot permite usar set_multiplayer_authority(peer_id) para indicar que cierto cliente es dueño de un nodo. Sin embargo, siguiendo server autoritativo, tal vez no hagamos eso y dejamos server owner de todos. En cualquier caso, el server al crear un nuevo jugador puede enviárselo al cliente (RPC al nuevo peer con rpc_id(new_peer, "assign_player", player_node_path) o similar).
  - Más sencillo: usamos un RPC calificado como any_peer[37] para que el cliente pueda pedir spawnear su jugador. Por ejemplo, el cliente tras conectar envía "ready_to_spawn", el servidor al recibir crea el Player y lo configura.
  - También manejar desconexiones: si un cliente se va (peer_disconnected señal), eliminar su nodo Player del juego.
  - Aunque todo esto es más sistema multijugador que jugabilidad, hay que implementarlo en esta etapa para poder probar el cooperativo. Se dividirá en tareas específicas luego.
  - Sala de espera: Dado que queremos mínimamente "que se vean en una sala", podríamos implementar una simple sala de espera donde los jugadores aparecen antes de que empiece la acción. Sin embargo, en endless, normalmente se empieza de inmediato. Quizá se interpreta "sala" como simplemente la misma arena. Clarificaremos que por ahora no habrá nivel de lobby, solo el juego en sí donde todos aparecen desde el inicio o al conectarse.

Resumiendo, todas estas funcionalidades estarán listas para la Versión 1.0 del juego. A continuación, estructuramos estas tareas en pasos concretos para su desarrollo.

## Plan de Implementación por Tareas ⏳

Para llevar a cabo el proyecto de forma organizada, dividiremos el trabajo en una serie de tareas concretas y medibles. Cada tarea corresponde a una funcionalidad o módulo descrito anteriormente. El orden propuesto busca primero establecer la base single-player y luego incorporar la capa de red:

- Configuración Inicial del Proyecto:
  - Crear el proyecto Godot 4 (Mono) desde cero. Configurar el entorno de C# en Godot (verificar que se genera la solución .sln, etc.).
  - Establecer la estructura de carpetas (scenes, scripts, assets) conforme a las buenas prácticas decididas.
  - Configurar en el Project Settings los Input Actions necesarias: por ejemplo, move_up, move_down, move_left, move_right para movimiento (o usar "ui_up" etc.), y shoot para disparar. Si usamos ratón para disparar, también click_left etc. (Inicialmente quizás todo teclado).
  - (Opcional) Integrar repositorio Git e inicializar README.md describiendo el proyecto.

Criterio de finalización: Proyecto creado, editor Godot funcionando con VSCode, acciones de input definidas, sin errores. Se puede ejecutar la escena vacía Main sin fallos.

Escena Principal (Main) y Nodo Game:

- Crear Main.tscn con un Node como root. Añadir como hijo un Node2D llamado Game que contendrá el mundo.
- Anclar desde el Project Settings esta escena como la principal (Main Scene) para ejecutar.
- Preparar dentro de Game un nodo vacío Enemies (Node2D) para alojar enemigos dinámicamente, y quizás colocar en el editor unos 2-4 Position2D como spawn points en los bordes.

Criterio: al ejecutar el proyecto (aunque aún no haya jugadores), la escena Main carga correctamente. Este es más un setup estructural.

Jugador – Movimiento básico (Single-player):

- Diseñar la escena player.tscn: root CharacterBody2D llamado "Player". Añadirle un Sprite2D (temporario, un rectángulo o icono) y un CollisionShape2D (un círculo o caja alrededor del sprite) para colisiones.
- Crear el script Player.cs (o .gd) y anexarlo al Player. Implementar en _PhysicsProcess(delta) la lectura de input: calcular un Vector2 dirección según teclas (ej: Vector2(dirX, dirY) donde dirX = derecha-izquierda, dirY = abajo-arriba)[38]. Normalizar e velocidad para obtener velocity y llamar MoveAndSlide() (método C# equivalente)[39]. También actualizar rotation del nodo hacia la dirección de movimiento si no es cero[40].
- Comprobar movimiento en juego: Instanciar manualmente un Player en la escena Main (temporal para pruebas singleplayer). Ejecutar y verificar que se mueve correctamente dentro de los límites.

Criterio: El personaje se mueve con fluidez con las teclas, se detiene al soltar, rota adecuadamente.

Disparo Local – Implementación de Bala:

- Crear la escena bullet.tscn: root Area2D "Bullet" con CollisionShape2D (círculo pequeño) y opcionalmente un Sprite (pequeño punto). Añadir también un subnodo VisibilityNotifier2D (o VisibleOnScreenNotifier2D) para detectar cuándo sale de pantalla.
- Script Bullet.gd (podemos usar GDScript aquí si se prefiere rapidez, aunque se puede en C# también). En _PhysicsProcess(delta), mover la bala en su dirección: Position += direction.Normalized() * speed * delta[41]. Conectar la señal del VisibilityNotifier "screen_exited" para que al salir llame queue_free()[32]. También conectar señal body_entered del Area2D para detectar colisiones[42]; si el cuerpo entrante pertenece al grupo "enemigo", destruir al enemigo (body.queue_free()) y destruirse a sí misma[42].
- Exportar en Bullet un valor speed = 400 (por ejemplo) y en Player script una variable export bullet_scene (PackedScene) para referenciar el prefab de bullet[43].
- En el script Player.cs, implementar la función de disparo. Por simplicidad, en _PhysicsProcess detectar si se pulsa la acción shoot (con Input.IsActionJustPressed("shoot") en C#)[44]. Si sí, instanciar la escena bala: var bullet = bulletScene.Instantiate<Bullet>(); (en C# genérico)[45]. Asignar bullet.Position = this.Position; bullet.direction = last_direction (last_direction almacenado cuando el player se movió por última vez)[46][47]. Agregar la bala como hija del mundo (podemos hacer GetParent().AddChild(bullet) asumiendo Player está bajo Game)[47].
- Probar en singleplayer: disparar y ver que las balas salen en la dirección adecuada, atraviesan la pantalla y se destruyen al salir. Sin enemigos aún, solo verificar que se crean y se limpian.

Criterio: Al pulsar disparo, se generan balas desde la posición del jugador, que viajan en línea recta y desaparecen al salir de la ventana (verificando en el remoto del engine o debug que se liberan).

Múltiples Armas (Pistola vs. Ametralladora):

- Extender la lógica de disparo para soportar dos modos:
  - Pistola: un disparo por pulsación. (Ya estaría cubierto con IsActionJustPressed).
  - Metralleta: disparo automático al mantener pulsado. Para esto, podemos usar Input.IsActionPressed("shoot") y un temporizador o acumulador de tiempo para espaciar los disparos. Por ejemplo, en Player añadimos una variable fire_rate = 5 disparos/seg -> intervalo 0.2s. Cada _PhysicsProcess, si el botón sigue presionado y ha pasado >0.2s desde el último disparo, instanciamos otra bala. Alternativamente, usar un Timer node para el ritmo.
  - Permitir cambiar de arma: quizás con otra tecla (ej. tecla "Q" para alternar pistola/metralleta). Implementar un simple toggle o enum de arma actual en Player.
  - Ajustar diferencias: la pistola podría hacer más daño o ser más precisa (esto en esta versión no se refleja, porque no hay sistema de daño acumulativo, simplemente 1 impacto = 1 kill). Podríamos ignorar la diferencia de daño por ahora. La principal diferencia es cadencia.

Criterio: Se puede alternar arma y al probar, la pistola solo dispara cuando se pulsa repetidamente, mientras la metralleta dispara ráfagas al mantener pulsado (verificar que no excede la cadencia establecida).

Enemigo – Escena e IA básica:

- Crear enemy.tscn: root CharacterBody2D "Enemy" (o "Zombie"), con Sprite2D (imagen de zombie) y CollisionShape2D apropiado. Añadirlo al grupo "enemigo" en el editor (Godot permite asignar grupos por nodo).
- Script Enemy.cs (o .gd): en _PhysicsProcess, si existe un jugador objetivo, calcular vector dirección hacia él y mover: velocity = (target.Pos - my.Pos).Normalized() * speed; MoveAndSlide()[35]. También rotar hacia la dirección para que apunte al jugador. Necesitamos obtener referencia al jugador; podemos buscarlo en _Ready(). Si suponemos un solo jugador, hacer player = GetTree().GetRoot().FindChild("Player", true, false) para encontrar el nodo Player[48]. Si hay varios jugadores, podríamos buscar todos en grupo "players" y elegir el más cercano cada frame (coste bajo con pocos players). Para ahora, podríamos simplemente asignar el primer jugador.
- Definir velocidad del enemigo (ej. 100) exportada para poder ajustar[49].
- (Sin implementar daño aún) Probar instanciando un Enemy manualmente en la escena, para ver si sigue al jugador correctamente.

Criterio: Un enemigo en escena se orienta y desplaza siguiendo la posición del jugador. Si el jugador se mueve, el enemigo lo persigue.

Gestión de Oleadas (WaveManager):

- En la escena principal (Main->Game), crear un Node "WaveManager". Añadir como hijos de este Node varios Position2D marcando posibles ubicaciones de spawn en el nivel (ya puestos en paso 2).
- Script WaveManager.cs:
  - Variables: PackedScene enemyScene exportado, int zombies_per_wave = 3 export, float time_between_waves = 5.0, contador current_wave = 1.
  - En _Ready(), recoger los spawn points (e.g. GetChildren() para lista de nodos de spawn[33]). Inmediatamente llamar a iniciar la primera oleada.
  - Método StartWave(): Generar zombies_per_wave * current_wave enemigos[50]. Para cada uno, elegir un spawn aleatorio de la lista, instanciar Enemy, posicionarlo en ese punto, añadirlo como hijo de Enemies (no de WaveManager, sino del contenedor global de enemigos en Game; podemos obtenerlo via GetNode("/root/Main/Game/Enemies") o mejor pasar referencia). Incrementar current_wave.
  - Luego iniciar un temporizador para lanzar la siguiente ola tras time_between_waves segundos[12]. En C#, podríamos usar async/await con SceneTreeTimer, o manual con un Timer node.
  - Conectar señales si se usa Timer, etc.
  - Probar en singleplayer: al iniciar, debería spawnear 3 enemigos; tras 5 segundos, spawnear 6, etc. Asegurar que las instancias aparecen en la escena y persiguen al jugador (ya que su AI debería activarse).

Criterio: Oleadas de enemigos aparecen de manera infinita, con cantidad creciente. Visible al verificar en el Remote Scene tree que se añaden enemigos bajo Enemies y el contador current_wave aumenta.

Combate – Integración Balas y Enemigos:

- Ahora conectar todo: cuando las balas toquen enemigos, que los eliminen:
  - Ya se programó en Bullet.gd que si colisiona con body.is_in_group("enemigo") hace body.queue_free()[10]. Confirmar que los enemigos están efectivamente en ese grupo (asignarlo en escena o vía código).
  - Verificar que al disparar a un enemigo, este se elimina de la escena.
  - Posiblemente también incrementar un contador de bajas (podemos en Bullet, justo antes de queue_free del enemy, emitir una señal global "enemy_killed" o llamar a un método del WaveManager/Game para contabilizar).
- Implementar daño al jugador por contacto:
  - En Enemy, conectar su body_entered para detectar colisión con Player (player podría tener un CollisionShape de tipo body). O más sencillo: en Player, conectar area_entered (si Player tuviera un Area, pero es CharacterBody2D... quizá mejor al revés: hacer que enemigos al tocar al Player se destruyan y reduzcan vida).
  - Una simple solución: en _PhysicsProcess de Enemy, si DistanceTo(player) < threshold (muy cercano), considerar que lo alcanzó. Pero eso es menos fiable que colisión real. Podríamos añadir un Area2D a Enemy para daño, o a Player.
- Por simplicidad, podríamos skip esta parte en la muy primera versión, pero como endless debe acabar, mejor incluir:
  - Dar a Player una propiedad health = 3.
  - En Enemy, al colisionar con Player (podemos detectar si el Player tiene una CollisionShape Body2D), restar salud al Player (via calling a method on Player or via group "players": e.g. enemy can do player.take_damage(1)).
  - Si health llega a 0, señal de "player_dead". En un solo jugador, terminar juego (mostrar Game Over). En multi, quizás un jugador puede morir y espectar al otro; si todos mueren, fin.
  - Estas mecánicas las podemos anotar pero su implementación completa podría ser un extra.

Criterio: El jugador puede eliminar enemigos disparando (enemigos desaparecen cuando son alcanzados). Si implementamos salud, el jugador pierde vida al ser tocado por un enemigo; eventualmente se podría forzar un Game Over manual para prueba (ej. log en consola).

Interfaz HUD básica:

- Añadir en la escena UI un Label para mostrar, por ejemplo, "Oleada: X" y otro "Enemigos eliminados: Y". También quizás una barra de vida (3 corazones o barra).
- Script simple HUD.cs que exponga métodos: UpdateWave(int wave) y UpdateKills(int kills) y UpdateHealth(int hp), para refrescar los textos.
- En WaveManager, tras iniciar cada ola, llamar a HUD.UpdateWave(current_wave). En Bullet, cuando mata a un enemigo, incrementar un contador global (quizá en Game) y llamar HUD.UpdateKills.
- En Player, cuando se daña, llamar HUD.UpdateHealth.

Criterio: Durante gameplay, se ve actualizar el número de oleada adecuadamente, y el conteo de kills aumenta con cada enemigo abatido.

Implementación de Red – Servidor/Cliente:

- Esta es una de las partes más críticas: programar el NetworkManager. Si lo hicimos Autoload, tendremos un script global accesible; si es un nodo en Main, tendremos que hacer GetNode("NetworkManager") para llamarlo.
- Añadir funciones:
  - StartServer() – crea un peer ENetMultiplayerPeer, escucha en puerto (ej. 7777) para un número máx de jugadores (ej. 4)[18]. Asignar GetTree().MultiplayerPeer = peer. Conectar señales peer_connected y peer_disconnected del MultiplayerAPI a callbacks.
  - JoinServer(ip) – similar pero usando create_client(ip, port)[51].
  - En Godot 4, se puede también usar MultiplayerAPI directamente. En C#, sería MultiplayerServer ms = new MultiplayerServer(); ms.Listen(port, max); GetTree().Multiplayer = ms; etc. Pero el ENetPeer está bien.
- Callbacks:
  - OnPeerConnected(int id): Si id != 1 (1 es el servidor), significa un cliente nuevo se ha conectado. Entonces instanciar un Player para ese cliente. Crear Player node, añadirlo a Game, y asignar su network authority: playerNode.SetMultiplayerAuthority(id) si quisiéramos que ese cliente controle algo. Sin embargo, manteniendo server autoritativo, podríamos no asignar authority y simplemente manejar inputs manualmente. Otra manera: aún si server es autoritativo, podemos darle al cliente autoridad sobre su Player node para que pueda enviar RPCs "any_peer" desde ese node. En Godot 4, por defecto server is authority, pero se puede permitir RPC from client with @rpc(any_peer) on input functions[37].
  - De cualquier forma, necesitamos una referencia entre el peer id y su Player node. Podríamos usar un Dictionary<int, Player> en NetworkManager para mapear.
  - También, si este juego tuviera login/nombres, aquí podríamos asignar un nombre o color a cada jugador.
  - Enviar un mensaje de bienvenida al cliente? Podríamos RPC de vuelta al cliente para confirmarle su spawn. Godot también ofrece la señal connected_to_server en el cliente para saber que ya está dentro[52].
  - OnPeerDisconnected(int id): Eliminar el nodo Player correspondiente a ese id (si existe en el diccionario). Liberar recursos.
- Entrada de Clientes: Hay que decidir cómo las entradas de movimiento/disparo del cliente llegan al server. Implementación:
  - Podemos aprovechar la anotación de RPCs. Por ejemplo, en Player.cs definimos: [Rpc(CallLocal = false, AnyPeer=true)] void RecibirInput(Vector2 dir, bool shooting); el cliente llama este RPC en su nodo Player remoto. Con AnyPeer, el server podrá ejecutarlo[37]. Otra forma: en NetworkManager, tener un método [Rpc(any_peer)] HandleInput(int peerId, Vector2 dir, bool shoot) que reciba para un peer dado.
  - Para mantenerlo simple: quizá cada frame, en el Player.cs del cliente local, en _PhysicsProcess, en lugar de mover directamente, envia RpcId(1, "PlayerInput", velocity) al server (1 es server). El servidor al implementar PlayerInput(peer, vel) aplicaría esa vel a ese peer's player node.
  - Este es un sistema manual pero claro. Alternativamente, Godot 4 permite hacer MultiplayerSynchronizer en Player to sync velocity automatically, pero sigamos manual para control.
  - Implementar también RPC para disparo: Cliente hace RpcId(1, "PlayerShoot") cuando dispara. El server, al recibir, ejecuta la creación de bala (similar a como hacía local) pero ahora en contexto multi.
- Difusión a clientes: Cuando el server mueve a un jugador o enemigo, tiene que informarlo. Godot puede sincronizar transform automáticamente si el node tiene PhysicsObject and sync, pero supongamos manual:
  - Tras server actualizar posición de Player, podría llamar Rpc("UpdatePlayerPos", peerId, newPos) a todos para que cada cliente ejecute eso en su correspondiente player node. Pero tienen que encontrar qué node mover. Podríamos RPC specifically to that client's player node as well.
  - Posiblemente más sencillo: en Godot 4, one can use @rpc(sync) on a variable (not sure if implemented). Otherwise, use playerNode.rpc("SetTransform", transform) to all. But since all clients have their own instance of that player node (with same name in scene tree), an RPC on that node from server will run on each client's corresponding node if paths align.
  - Podríamos simply rely on the fact that if server moves the Player node and the node is networked, maybe a synchronizer can handle.
  - Este detalle puede requerir iteración, pero para el plan diremos: utilizaremos RPCs para notificar cambios relevantes (spawn/enemy death).
  - Spawn inicial: Cuando el server inicia (StartServer), debe crearse su propio jugador (ya que peer id 1 es server). Entonces manualmente instanciar Player para servidor y quizás marcarlo de alguna forma (aunque no estricto).
- Testing: Al terminar esta tarea, deberíamos poder:
  - Iniciar una instancia en modo servidor (p.ej. mediante un parámetro o un botón).
  - Iniciar otra instancia en modo cliente que se conecte al servidor.
  - Ver en la ventana del server que apareció el Player del cliente, y en la del cliente que ve tanto al jugador server como a sí mismo.
  - Mover cada uno en su instancia y ver el movimiento replicado en la otra ventana.
  - Disparar y ver que las balas aparecen en ambos.
  - Enemigos: correr la lógica de spawn en server y verificar que clientes ven a los enemigos y pueden dispararles, y que al morir desaparecen en ambos.
- Esta tarea es grande; se puede subdividir:
  - 10.a: Networking Base – Implementar StartServer, JoinServer, conexiones y spawns de jugadores (sin movimiento aún).
  - 10.b: Sync Jugadores – Manejar input RPC y movimiento replicado.
  - 10.c: Sync Disparos – Manejar RPC disparo -> spawn bala en server -> replicar bala.
  - 10.d: Sync Enemigos – Replicar spawn y movimiento de enemigos a clientes.
  - Criterio: Esta etapa se logra cuando logramos una partida con 2 instancias donde ambos jugadores se mueven y disparan y ven lo mismo. Es decir, el estado del juego está compartido correctamente (al margen de pequeñas latencias). Cada jugador ve al otro moverse; los enemigos aparecen para ambos; si uno dispara un enemigo, también desaparece en el otro.

Pulido y Pruebas Finales:

- Probar con diferentes números de jugadores (hasta el límite definido, por ejemplo 2 o 4) en red local, monitoreando estabilidad.
- Ajustar parámetros de juego: velocidad de enemigos vs. jugadores para asegurar que es jugable, cadencia de disparos, tiempo entre oleadas (quizá inicial muy corto para test, luego se puede subir para darle respiro).
- Mejorar alguna funcionalidad si quedó tosca: por ejemplo, interpolar movimiento de enemigos en clientes si van a saltos, o corregir la lógica de selección de jugador objetivo para que no todos los zombies vayan por uno solo siempre.
- Limpiar el código: remover impresiones debug, asegurar comentarios y organización coherente.
- Documentar brevemente en el README cómo lanzar un servidor y clientes (instrucciones de ejecución).
- Criterio: El juego corre varios minutos sin errores graves, la sincronización se mantiene. Los jugadores pueden jugar juntos endless mode. El código está ordenado, listo para futuras modificaciones.

Nota final: Gracias a la planificación cuidadosa y a las prácticas recomendadas (estructura por escenas, servidor autoritativo, etc.), este proyecto se establece con bases sólidas. Godot nos brinda facilidades (RPC de alto nivel, etc.) que simplificarán varias implementaciones de red[53], permitiendo centrar el esfuerzo en la jugabilidad. A medida que el desarrollo avance, siempre podremos iterar sobre esta arquitectura modular para agregar nuevos modos de juego, armas, tipos de enemigos o mejorar el netcode (por ejemplo, optimizaciones de lag). Pero con este plan de proyecto claro, dividido en tareas manejables, estamos listos para empezar a construir Endless Multiplayer Shooter 🎮🚀.

[1] [14] [15] [16] [17] [22] [23] [24] [25] [26] [27] [28] Andrew Davis - Godot Multiplayer: 3 Quick Tips for Better Netcode

https://jonandrewdavis.com/drafts/draft-of-godot-network-tips/

[2] [3] [4] GDScript vs C# in Godot 4

https://chickensoft.games/blog/gdscript-vs-csharp

[5] [18] [19] [20] [21] [36] [37] [51] [52] [53] High-level multiplayer — Godot Engine (stable) documentation in English

https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html

[6] [8] [9] [13] GitHub - abmarnie/godot-architecture-organization-advice: Advice for architecting and organizing Godot projects.

https://github.com/abmarnie/godot-architecture-organization-advice

[7] How To Structure Your Godot Project (so You Don't Get Confused)

https://pythonforengineers.com/blog/how-to-structure-your-godot-project-so-you-dont-get-confused/index.html

[10] [31] [32] [41] [42] Bullet.gd

file://file-V1mUNWmkbAJCNRPBFwZeg1

[11] [12] [33] [34] [50] WaveManager.gd

file://file-EymQYZcmNfu51PGsHYMYJE

[29] [30] [38] [39] [40] [43] [44] [45] [46] [47] Player.gd

file://file-7Yc321BKp1Z53eGDtxSX6X

[35] [48] [49] Zombie.gd

file://file-73q2EEAnoLfV1Asg1umF1y