# Cazador de la Cripta - FPS con ballesta (Unity)

Juego de disparos en primera persona hecho en Unity 6 para la materia Programación de Video Juegos.

## Autor

- **Estudiante:** Edwin Jesús Flete Cruz
- **Matrícula:** 1-22-1805
- **Universidad:** UTESA, Sede Santiago
- **Asignatura:** Programación de Video Juegos (INF-910-001)
- **Profesor:** Iván Mendoza
- **Motor:** Unity 6 (6000.6.4f1), URP, C#, Input System

## Enlaces

- **Jugar en el navegador:** https://wingsofrebel.github.io/Cazador-de-la-Cripta/
- **Repositorio:** https://github.com/WingsOfRebel/Cazador-de-la-Cripta

## Controles

| Acción | Teclas |
|---|---|
| Moverse | Flechas del teclado (también W A S D) |
| Apuntar | Ratón |
| Disparar / golpear con la espada | Clic izquierdo |
| Cambiar de arma (ballesta / báculo / espada) | 1 / 2 / 3 o rueda del ratón |
| Acercar la vista (zoom) | Clic derecho |
| Correr / Saltar | Shift / Espacio |
| Pausa | Esc |

## Niveles

1. **01 Galeria de disparos:** 10 dianas con distintos puntajes (diana 10, lejana 15, de pared 15, pequeña 25, de pared pequeña 30, móvil 50). Los barriles restan 10 y las botellas dan 5. Hay 15 flechas.
2. **02 Mazmorra:** 9 esqueletos (esbirros, pícaros, guerreros y un mago) que te persiguen. Si te tocan, pierdes una vida. Hay carcajes con flechas y una poción de vida.
3. **03 Jefe:** el Rey Esqueleto (30 de vida), que llama a sus esbirros cada cierto tiempo.

## Reglas

- Empiezas con 3 vidas. Cada nivel te da munición nueva (15, 20 y 25).
- Hay tres armas: la **ballesta** (flechas con un poco de caída, daño 2) y el **báculo** (orbes mágicos rectos y rápidos, daño 1) gastan la misma munición; la **espada** golpea de cerca (daño 3) y no gasta munición.
- Un golpe a la **cabeza** hace daño crítico (el doble) y da 25 puntos extra.
- Al acertar una diana se destruyen la diana y la flecha, y sumas los puntos de ese objeto.
- Pierdes si te quedas sin vidas (te tocan los esqueletos) o sin flechas antes de cumplir el objetivo.
- Al final aparece un menú con la puntuación, las vidas restantes y los botones **Volver a jugar** y **Menú principal**. La puntuación también se escribe en la consola.

## Estructura (`Assets/Scripts`)

- `Nucleo/` Juego, Nivel, GestorAudio
- `Jugador/` JugadorFPS, Ballesta (armas), GolpeEspada, VistaArma
- `Enemigos/` EnemigoEsqueleto (también el jefe)
- `Objetos/` Flecha, Objetivo, Recogible, MovimientoVaiven, Astillas, Parpadeo
- `UI/` HUD, MenuPrincipal
- `Assets/Editor/ConstruirJuego.cs`: menú **FPS > Construir todo**, que arma los prefabs y las 4 escenas

## Créditos

- Esqueletos, ballesta, báculo, espada, flechas y carcaj: **KayKit Skeletons** de Kay Lousberg (CC0).
- Mazmorra: **KayKit Dungeon Pack** de Kay Lousberg (CC0).
- Dianas: **KayKit Prototype Bits** de Kay Lousberg (CC0).
- Música y efectos de sonido: hechos para este proyecto (generados por código).
