# Reporte: Práctica N.° 5, Unity 6 + XR

**Tema:** escena inmersiva con iluminación pre-calculada (lightmapping) y audio espacial.
**Proyecto:** `Practica05_XR_Inmersion`
**Escena:** `Assets/Scenes/Practica05.unity`
**Motor:** Unity 6000.3.10f1 · Universal 3D (URP 17.3)
**Prueba:** en PC, con el simulador de XR Interaction Toolkit (XR Interaction Simulator / XR Device Simulator)

## Mini reporte

| Dato | Valor |
|---|---|
| Nombre del proyecto | Practica05_XR_Inmersion |
| Tiempo de bake | **00:00:29** (29,43 s; bake limpio con la caché de GI borrada, Progressive GPU) |
| FPS en Play Mode | **Promedio 104,2 FPS** · mediana 111,6 · máximo 118,8 · percentil 5: 63,4 (42 muestras, 1 por segundo) |
| Tipo de luz | **Baked** (Directional, Area, 2 Point y 1 Spot) |
| Audio 3D | **Spatial Blend 1.0 + Logarithmic Rolloff** (Audio_Object: Min 1 m / Max 15 m) con HRTF de Meta XR Audio |
| Equipo | ASUS Vivobook M1605YA · AMD Radeon Graphics (integrada) |

**Conclusión:** sí, la escena se siente más presente. La iluminación bakeada da sombras suaves y estables, sin parpadeo, y no gasta GPU en cada cuadro. El audio espacial permite saber dónde está la radio y encontrar el objeto invisible sin verlo. Al girar la cabeza el sonido cambia de oído, y al acercarse sube de volumen: en la prueba pasó de 17 % a 4,8 m a 72 % a 1,1 m.

> El FPS mínimo absoluto registrado (10,1) es un pico aislado causado por la herramienta de automatización del editor al ejecutar código. Por eso se informan la mediana y el percentil 5.

## Desarrollo paso a paso

| Paso del tutorial | Implementación |
|---|---|
| 1. Proyecto Unity 6 (URP) | Proyecto Universal 3D. Escena guardada en `Assets/Scenes/Practica05.unity`. |
| 2. XR Interaction Toolkit | XRI **3.6.1** instalado con el Input System nuevo activo. También se añadió **OpenXR 1.18** para usar un visor más adelante. |
| 3. Starter Assets y simulador | Samples **Starter Assets**, **XR Device Simulator** y **XR Interaction Simulator** importados en `Assets/Samples`. |
| 4. Escena sencilla | **Floor** (Plane), **Wall_North/South/East/West** (Cubes; Wall_North con ventana), techo y muebles: cama, velador con lámpara, escritorio, silla, estante, ropero, alfombra, cuadro y planta. También **Cube_Interactuable** y **Audio_Object**. |
| 5. Static | Toda la arquitectura y los muebles son Static (Contribute GI, Batching, Occluder, Occludee y Reflection Probe). **Cube_Interactuable** y **Audio_Object** no son estáticos porque se pueden agarrar y mover. |
| 6. Luces Baked | Directional (intensidad **1.0**), Area de ventana, Point de techo, Point de velador y Spot de escritorio. Todas en modo **Baked**. |
| 7. Lighting y bake | Lightmapper **Progressive GPU**, 20 texels/u, máximo 1024, sin direccionalidad, AO y 2 rebotes. Lightmap Parameters `LP_Movil_Quest` pensados para visores móviles. Bake de **29,43 s**. |
| 8. Revisar lightmaps | 1 lightmap de 512×512 (`Lightmap_0.png`). Hay 75 Light Probes para los objetos dinámicos y un Reflection Probe bakeado. |
| 9. Audio Source 3D | **Audio_Object** (radio) con clip en loop, Play On Awake y Spatial Blend **1.0**. |
| 10. Atenuación | **Logarithmic Rolloff**, Min Distance **1**, Max Distance **15**. |
| 11. XR Origin y simulador | **XR Origin (XR Rig)** con una sola cámara y un solo **Audio Listener**, más el simulador para PC. Se eliminó la Main Camera original. |
| 12. Ejecutar y comprobar | Se verificó el cambio de volumen y dirección (tabla siguiente) con los valores del HUD. La segunda prueba oculta **Audio_Object** con la tecla **V** y el sonido sigue. |

### Prueba de volumen y dirección (Play Mode)

| Posición del usuario | Audio_Object | Volumen estimado | Dirección percibida |
|---|---|---|---|
| Lejos (lado oeste) | 4,8 m | 17 % | delante-centro |
| Cerca, mirando al norte | 1,1 m | 72 % | derecha |
| Cerca, girado 180° | 1,1 m | 72 % | izquierda |

### Mejoras sobre el tutorial
- **Spatializer HRTF:** Meta XR Audio, activado en *Project Settings > Audio*, con sala acústica de 6 × 2,8 × 5 m y sus materiales.
- **Objeto_Invisible:** fuente sin malla que solo se localiza por el sonido. Al acercar la cabeza a menos de 0,6 m se revela y cambia a otro de 5 escondites.
- **Interacción:** la radio y el cubo se agarran con el grip, y el gatillo enciende o apaga la radio.
- **Post-procesado apto para XR:** Bloom suave, Tonemapping Neutral y Color Adjustments. Se excluyen Motion Blur, Chromatic Aberration y Lens Distortion porque pueden causar mareo.
- **HUD en el mundo:** muestra FPS y la distancia, volumen y dirección de cada fuente (tecla H). Guarda el registro en `fps_log.csv`.
- **Menú Practica5:** Construir Escena, Bakear Iluminación y Exportar Lightmaps (reproducible). Cada bake se registra en `bake_report.txt`.

## Controles en PC (simulador)
- **WASD:** moverse. **Clic derecho + ratón:** girar la cabeza.
- **Tab:** cambiar entre cabeza, mando izquierdo y mando derecho. **G:** grip (agarrar). **T:** gatillo (encender o apagar la radio).
- **V:** ocultar o mostrar Audio_Object. **H:** mostrar u ocultar el HUD.
- Se recomienda usar audífonos.

## Preguntas orientadoras
**¿Por qué marcar objetos como Static?**
Unity puede incluirlos en el bake de iluminación, el batching estático y el occlusion culling. En un visor autónomo eso evita repetir en cada cuadro trabajo que ya se calculó.

**¿Cómo ayuda el audio espacial?**
Da pistas de dirección y distancia. Así el usuario localiza eventos fuera de su campo visual sin giros bruscos, y la experiencia resulta más coherente.

**¿Qué diferencia hay entre Baked y Mixed?**
Baked pre-calcula la luz directa e indirecta sobre los objetos estáticos y no tiene costo en tiempo de ejecución. Mixed combina lo pre-calculado con luz o sombras en tiempo real para objetos dinámicos, y eso tiene más costo.

## Problemas encontrados y solución
| Problema | Solución aplicada |
|---|---|
| Materiales magenta en los mandos del Starter Assets | Se convirtieron a URP/Lit, y las líneas de los rayos a URP Particles Unlit. |
| Sonido con chasquidos | Los clips se rehicieron con envolventes de ataque y release y loop sin cortes. |
| Reverberación genérica | Se añadió una Meta XR Audio Room con las medidas reales de la habitación. |
| Dos cámaras o dos Audio Listeners | Solo queda la cámara del XR Origin, con un único Audio Listener. |
| Aviso "Failed to get haptic capabilities" en consola | Los mandos simulados no vibran. `SimulatorHapticsGuard` desactiva la vibración solo cuando no hay un visor real. |

## Evidencias
- `Vista_Habitacion_Bakeada.png`: resultado del bake.
- `Lightmap_0.png`: lightmap generado.
- `PlayMode_Simulador.png`: prueba en Play con el simulador y el HUD.
- `bake_report.txt`, `fps_medicion_30s.csv` y `fps_medicion_30s_resumen.txt`: datos de tiempos y FPS.
