# Foco y activación de objetos

## Comportamiento

- **SpearsButton:** primera pulsación → llegada de cámara → habilitación del peso que mueve las lanzas. Las pulsaciones siguientes actúan directamente. Ver [Spears y SpearsButton](Spears-y-SpearsButton.md).
- **Eagle / Button:** el impacto cierra el Eagle y deshabilita su collider inmediatamente. El primer `FocusOnActivation` válido de su lista de plataformas proporciona el único encuadre. Al llegar, todas las plataformas conectadas se activan juntas. Se ignoran entradas vacías y duplicadas.
- **Plataformas:** movimiento, brillo y partículas comienzan juntos; el brillo ya no retrasa el recorrido. Se mantienen velocidades, waypoints, duraciones y la configuración de movimiento al iniciar la escena.
- Durante el foco, el grupo activado puede moverse y las plataformas ajenas siguen congeladas. La pausa y cualquier bloqueo adicional, como muerte, siguen deteniendo al grupo.

No hace falta reconectar las escenas. El orden de la lista del Eagle determina qué encuadre se elige. Las plataformas siguen pudiendo usar `StartAction()` individualmente; llamarlo estando activas las detiene.

## Contrato compartido

`FocusOnActivation.ActivateWhenFocused(owner, onReady, onFinished)` coordina la acción con `FocusManager`. `onReady(bool hasFocus)` se ejecuta una sola vez después de una actualización del Brain con la cámara de foco activa y sin transición de entrada. `focusDuration` empieza en ese momento. Un corte instantáneo también espera esa actualización.

Si no hay cámara/configuración válida, o `onlyOnce` ya se consumió, `onReady(false)` ejecuta la acción directamente. `onFinished` permite retirar la excepción de movimiento del grupo. El método devuelve un `ActivationHandle` cancelable; `Dispose()` finaliza la solicitud sin ejecutar una acción pendiente. Desactivar el componente de foco o su solicitante cancela la solicitud. Una segunda llamada al mismo foco mientras su solicitud está pendiente no duplica la acción.

Los controladores de plataformas implementan `IFocusActivatablePlatform` para que el Eagle pueda iniciar el grupo sin generar focos adicionales. `PlayerLock.IsLockedExcept(FocusManager.LockId)` permite ignorar únicamente el bloqueo de ese foco durante la presentación del grupo.

`Activate()`, `RequestObjectFocus()`, `RequestRevealFocus()` y los tutoriales conservan el contrato previo: no incorporan la espera de llegada de la nueva activación.

## Verificación

En **Window → General → Test Runner → EditMode** están `SpearsRegressionTests` y `FocusActivationRegressionTests`. Entran en Play; guardar la escena antes de ejecutarlas. Las nuevas pruebas crean sus propios objetos, eventos y cámaras; ejecutarlas desde una escena vacía.

Verificado en Unity **6000.3.10f1**, con Cinemachine **2.10.5**:

- **22 pruebas aprobadas:** 10 regresiones de Spears y 12 de sincronización, grupos, bloqueos, cancelación, pérdida de cámara y compatibilidad.
- Compilación completa de los ensamblados de juego y editor sin errores. Las pruebas se ejecutaron en un proyecto temporal con copias de los scripts reales y el paquete Cinemachine del proyecto.
- La prueba de tiempo comprueba que el movimiento no comienza durante la entrada y que la permanencia configurada empieza al llegar. Las pruebas de grupo comprueban ambos controladores y que las plataformas ajenas se congelan.

Pendiente de comprobación visual en los niveles: encuadre artístico, materiales, sonidos y partículas de las instancias reales. Las pruebas automatizadas verifican la secuencia y el movimiento, pero no reemplazan ese recorrido jugable.
