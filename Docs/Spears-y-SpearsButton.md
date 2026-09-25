# Uso de Spears y SpearsButton

## Conectar los prefabs

1. Arrastrá **Spears** y **SpearsButton** desde `Assets/Prefabs/Obstacles` a la escena.
2. Seleccioná la raíz de **SpearsButton**.
3. En **Pressure Button Coordinator → Lanzas conectadas**, agregá un elemento por lanza y arrastrá la raíz de cada instancia de **Spears**. El destino es su componente **Spike Trap Controller**.
4. Para que varios botones controlen una misma lanza, repetí la conexión desde cada botón. No hace falta agregar otro componente ni configurar una lista inversa.
5. Guardá la escena. Las referencias entre instancias pertenecen a la escena: no se guardan como conexiones a objetos de escena dentro del asset del prefab.

Cada lanza suma solamente sus botones conectados. Las conexiones duplicadas cuentan una sola vez. Los orquestadores antiguos siguen funcionando; no hace falta eliminarlos ni volver a conectar las escenas.

## Configurar las lanzas

En **Spike Trap Controller**:

| Campo | Uso | Valor predeterminado |
|---|---|---|
| Peso para media altura | Suma mínima para la posición intermedia | 1 |
| Peso para bajar completamente | Suma mínima para bajar del todo | 2 |
| Posición arriba | Posición local de la raíz móvil | `(0, 0, 0)` |
| Posición a media altura | Posición local intermedia | `(0, -0.75, 0)` |
| Posición abajo | Posición local retraída | `(0, -2, 0)` |
| Duración del movimiento | Segundos de cada recorrido | 0.45 |

Con los valores predeterminados: **0 → arriba; 1 → media altura; 2 o más → abajo**.

Podés cambiar también la curva, dirección, amplitud, frecuencia y duración de la vibración. Las posiciones son relativas al padre de **Motion Root**, no coordenadas del mundo. Para mover todo el obstáculo, mové la raíz del prefab. Para cambiar sólo sus alturas, editá las tres posiciones del controlador.

El menú contextual del componente permite capturar la posición de Motion Root como cada una de las tres alturas. El estado inicial se usa al comenzar; cuando hay botones conectados, su suma determina el destino.

## Configurar los botones y la retención

### Foco de la primera pulsación

Cada **SpearsButton** solicita un solo foco por carga de escena, al alcanzar el primer estado habilitado en **Pressure Button One Shot Focus Trigger**. Media presión y presión completa comparten ese único disparo. Los campos existentes de estados habilitados y las referencias de cámara se conservan.

La placa, las runas y las partículas responden inmediatamente. El aporte a las lanzas espera hasta que Cinemachine termina la transición de entrada. Entonces se aplica el peso efectivo vigente: si cambió durante el recorrido de cámara, no se reproduce un valor anterior. Las siguientes pulsaciones y cambios de peso no esperan ni vuelven a enfocar. La vibración propia de la lanza se conserva.

`EffectiveWeight` y `EffectiveState` describen el botón. `TrapWeight` y `TrapState` describen el aporte habilitado para sus lanzas; durante el primer paneo pueden ser distintos. Tanto las conexiones directas como los orquestadores antiguos usan este aporte. Los temporizadores de retención mantienen sus reglas y no se reinician por el foco.

La configuración de cámara sigue en **Focus On Activation**. En esta secuencia, **Focus Duration** cuenta desde la llegada al encuadre, además del tiempo de entrada. Si el foco no está disponible, la activación continúa directamente y se registra una advertencia.

En **Pressure Button State Resolver**:

- **Peso para media pulsación:** 1 por defecto.
- **Peso para pulsación completa:** 2 por defecto.

Estos umbrales controlan la placa y cuándo se arma la retención de ese botón. Los umbrales de cada lanza se configuran por separado. El botón transmite su peso real, incluso si supera el umbral de pulsación completa.

En **Pressure Button Hold Timer → Retención (segundos)** configurá cuánto tiempo conserva la pulsación completa al perder peso. El prefab usa 10 segundos; las escenas conservan sus propios valores. Cambiar esta duración durante Play afecta a la siguiente cuenta regresiva.

Durante la retención, el botón aporta su umbral de pulsación completa, no el mayor peso que tuvo anteriormente. Si recupera suficiente peso, cancela la cuenta regresiva. La próxima pérdida inicia una cuenta nueva. Desactivar el botón cancela su retención y retira su aporte; al reactivarlo se vuelve a evaluar el peso presente.

En **Pressure Button Plate Mover** podés ajustar las tres posiciones locales de la placa, la duración y la curva. Usá la sección **Referencias internas (avanzado)** sólo si modificás la jerarquía del prefab. El sensor, la placa, el temporizador, las runas y las partículas ya están conectados dentro del prefab.

### Ejemplo: dos botones con peso 1

Conectá A y B a la misma lanza. Colocá un objeto de peso 1 sobre cada uno. La suma es 2 y la lanza baja completamente. Cada placa queda a media pulsación y no inicia una retención. Al retirar uno de los objetos, la suma baja a 1 y la lanza vuelve a media altura.

### Ejemplo: retención independiente

Un objeto de peso 2 pulsa completamente A. Al retirarlo, A sigue aportando 2 durante sus 10 segundos de retención. La lanza permanece abajo. Si B aporta 1, la suma durante esa espera será 3; al terminar será 1 y la lanza quedará a media altura.

Si una lanza requiere 5 para bajar y un botón con umbral completo 2 pierde una carga de peso 5, durante su retención aportará 2: la lanza evaluará ese nuevo total con sus propios umbrales.

## Dar peso a objetos nuevos

1. Agregá **Constant Weight Provider** al objeto o a un padre de sus colliders.
2. Configurá **Weight** con un entero no negativo.
3. Asegurá que el objeto tenga collider y que las capas permitan interactuar con el trigger del botón. El prefab del botón ya contiene la configuración física necesaria para su sensor.
4. Para objetos que caen o pueden empujarse, usá el Rigidbody correspondiente a esa mecánica.

No agregues un proveedor distinto por collider de un mismo objeto: todos deben encontrar al mismo proveedor en su jerarquía. Así su peso cuenta una sola vez en cada sensor.

El jugador existente utiliza **Player Weight Provider**: normal aporta 2, pequeño aporta 1 y los demás tamaños aportan 0. No necesita un Constant Weight Provider adicional. Para cambiar pesos desde otro script, `ConstantWeightProvider.SetWeight(int)` notifica a los sensores que ya detectan el objeto.

## Comprobar durante Play

- En el botón: consultá **Peso real**, **Aporte efectivo**, **Estado de la placa** y el progreso de retención.
- En la lanza: consultá **Peso total**, **Estado objetivo** y la lista de botones conectados activos con sus aportes.
- Los datos de diagnóstico son de solo lectura. Los cambios de configuración hechos durante Play normalmente no se conservan al salir.
- Un botón sin destinos funciona visualmente, pero no mueve lanzas. Un elemento vacío se ignora y se señala en el Inspector.
- Si no hay peso, revisá el proveedor del objeto, su collider, las capas y el sensor. Si hay peso pero no movimiento, revisá el destino, los umbrales y las referencias internas de la lanza.

## Verificación técnica

Las pruebas `SpearsRegressionTests` están disponibles en **Window → General → Test Runner → EditMode**. Entran temporalmente en Play para probar sensores, suma, retención, reactivación y movimiento. Guardá tu trabajo antes de ejecutarlas.

Las conexiones se registran mediante `RegisterButton(button, connectionOwner)` y se retiran mediante `UnregisterButtons(connectionOwner)`. Se deduplican por botón, conservando las distintas rutas. `EffectiveWeightChanged` informa cambios de aporte aunque no cambie la placa. `ISpikeTrapController.SetState` sigue disponible para consumidores de estados; los botones conectados a `SpikeTrapController` usan la suma. Otros controladores que sólo implementen esa interfaz conservan el comportamiento anterior por estados.

El sensor comprueba superposición antes de eliminar contactos sin eventos recientes, y reconstruye contactos al reactivarse. Referencias de Unity: [OnTriggerStay](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnTriggerStay.html) y [ComputePenetration](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.ComputePenetration.html).

### Resultado de la verificación de esta refactorización

- **Unity 6000.3.10f1: 10 pruebas aprobadas**, ejecutadas en un proyecto temporal con copias de los scripts reales, sin sustituir la física ni los temporizadores. Incluyen suma, deduplicación, retención, placa, reactivación, objetos dormidos, edición de conexiones, protección ante desbordamiento de la suma y datos del sensor usados por el balancín.
- **Compilación completa de Assembly-CSharp y Assembly-CSharp-Editor: sin errores**, usando el compilador y las referencias del proyecto en Unity 6000.3.10f1. Hay advertencias en código ajeno a estos cambios; ninguna en los scripts modificados o agregados.
- **Escenas revisadas sin modificarlas:** `Zone 1 - 1` y `Zone 1 - Boss` tienen cuatro conexiones directas de botón cada una, con destinos existentes. La primera también tiene un orquestador con dos botones. La escena Boss contiene un orquestador antiguo con referencias vacías: queda sin efecto y el Inspector lo advierte.
- **Pendiente de comprobación visual:** recorrido jugable en ambas escenas, cámara, runas, partículas, cambio de tamaño del jugador y movimiento completo del balancín. Sus scripts y valores no se cambiaron; la prueba del balancín verifica los datos de los sensores y su resolución izquierda/derecha, no su presentación completa en el nivel.
