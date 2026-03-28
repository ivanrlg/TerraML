# Como Funciona FuzzySat

> Una explicacion simple del proceso completo de clasificacion de imagenes satelitales
> usando logica difusa, desde el entrenamiento hasta el mapa final.

---

## La Idea en Una Frase

FuzzySat mira cada pixel de una imagen satelital, lo compara con ejemplos conocidos
(agua, bosque, urbano, etc.) y decide a cual clase se parece mas.

---

## Paso 1: La Imagen Satelital

Una imagen satelital no es como una foto normal. Tiene **multiples bandas espectrales**,
cada una capturando un rango diferente de luz:

```
Imagen ASTER (4 bandas):

Banda VNIR1  Banda VNIR2  Banda SWIR1  Banda SWIR2
(verde)      (rojo)       (infrarrojo)  (infrarrojo)
+--------+   +--------+   +--------+   +--------+
| 128 95 |   | 112 80 |   | 158 45 |   | 138 30 |
|  72 30 |   |  93 14 |   |  84 11 |   |  71  9 |
+--------+   +--------+   +--------+   +--------+
```

Cada pixel tiene **un valor por banda**. Un pixel en la esquina superior izquierda
tiene los valores: `VNIR1=128, VNIR2=112, SWIR1=158, SWIR2=138`.

Diferentes materiales reflejan la luz de forma diferente:
- **Agua**: refleja poco en todas las bandas (valores bajos)
- **Vegetacion**: refleja mucho en infrarrojo cercano (VNIR2 alto)
- **Urbano**: refleja bastante en infrarrojo de onda corta (SWIR alto)

---

## Paso 2: Entrenamiento (Aprender de Ejemplos)

Un experto selecciona **pixeles de ejemplo** donde sabe que hay agua, bosque, urbano, etc.
FuzzySat calcula dos numeros por cada clase y cada banda:

- **Media** (promedio): el valor tipico
- **Desviacion estandar**: que tan dispersos estan los valores

```
Ejemplo: 5 pixeles de entrenamiento para "Urbano" en la banda VNIR1:

Valores: 125, 130, 128, 135, 132

Media    = (125 + 130 + 128 + 135 + 132) / 5 = 130
Desv.Std = 3.39  (que tan lejos estan del promedio)
```

Resultado del entrenamiento (7 clases, 4 bandas = 56 numeros):

```
Clase       | VNIR1          | VNIR2          | SWIR1          | SWIR2
            | media  desv    | media  desv    | media  desv    | media  desv
------------|----------------|----------------|----------------|----------------
Urbano      | 130.0  18.0    | 110.0  15.0    | 160.0  22.0    | 140.0  20.0
Agua        |  25.0   8.0    |  15.0   6.0    |  10.0   5.0    |   8.0   4.0
Bosque      |  75.0  12.0    |  95.0  14.0    |  85.0  16.0    |  70.0  13.0
Agricultura |  90.0  15.0    |  80.0  12.0    |  70.0  18.0    |  60.0  14.0
...
```

---

## Paso 3: Funciones de Membresia (La Curva de Campana)

Con la media y desviacion estandar, FuzzySat construye una **curva de campana (Gaussiana)**
para cada clase y cada banda. Esta curva responde la pregunta:

> "Que tan compatible es este valor de pixel con esta clase?"

```
Formula:  mu(x) = exp( -0.5 * ((x - media) / desv)^2 )

Resultado: un numero entre 0 y 1
  - 1.0 = perfectamente compatible
  - 0.0 = totalmente incompatible
```

Ejemplo visual para "Urbano" en banda VNIR1 (media=130, desv=18):

```
Grado de
membresia
  1.0 |          *****
      |        **     **
  0.5 |      **         **
      |    **             **
  0.0 |__**_________________**____
      76   94   112   130   148   166   184
                      ^
              Valor del pixel (VNIR1)
```

- Un pixel con VNIR1 = 130 tiene membresia **1.0** (es el valor tipico de Urbano)
- Un pixel con VNIR1 = 112 tiene membresia **0.61** (se aleja un poco)
- Un pixel con VNIR1 = 76 tiene membresia **0.01** (muy lejos, casi seguro no es Urbano)

---

## Paso 4: Evaluar un Pixel (Inferencia Difusa)

Para clasificar **un pixel**, FuzzySat hace esto:

### 4a. Fuzzificar: evaluar todas las curvas

Supongamos un pixel con valores: `VNIR1=128, VNIR2=112, SWIR1=158, SWIR2=138`

```
                       VNIR1=128  VNIR2=112  SWIR1=158  SWIR2=138
                       ---------  ---------  ---------  ---------
Curvas "Urbano":         0.99       0.97       0.99       0.99
Curvas "Agua":           0.00       0.00       0.00       0.00
Curvas "Bosque":         0.02       0.17       0.01       0.00
```

### 4b. AND (minimo): la banda mas debil manda

Para cada clase, tomamos el **minimo** de todas las bandas.
Es como decir: "solo eres Urbano si te pareces en TODAS las bandas".

```
Fuerza "Urbano"  = min(0.99, 0.97, 0.99, 0.99) = 0.97
Fuerza "Agua"    = min(0.00, 0.00, 0.00, 0.00) = 0.00
Fuerza "Bosque"  = min(0.02, 0.17, 0.01, 0.00) = 0.00
```

### 4c. Defuzzificar: el ganador se lleva todo

La clase con la **fuerza mas alta** gana:

```
Resultado: URBANO (fuerza = 0.97)
```

Ese pixel se clasifica como **Urbano** con una confianza del 97%.

---

## Paso 5: Clasificar Toda la Imagen

FuzzySat repite el Paso 4 para **cada pixel** de la imagen:

```
Imagen original (valores de pixel):       Imagen clasificada:
+--------+--------+--------+              +--------+--------+--------+
|128,112 | 95, 80 | 30, 14 |              | URBANO |  AGRI  |  AGUA  |
|158,138 | 70, 45 | 11,  9 |              |  0.97  |  0.82  |  0.99  |
+--------+--------+--------+     --->     +--------+--------+--------+
| 72, 93 | 30, 20 | 90, 80 |              | BOSQUE |  AGUA  |  AGRI  |
| 84, 71 | 12, 10 | 68, 58 |              |  0.89  |  0.95  |  0.78  |
+--------+--------+--------+              +--------+--------+--------+
```

El resultado es un **mapa tematico** donde cada pixel tiene:
- Una **clase** (Urbano, Agua, Bosque, etc.)
- Una **confianza** (que tan seguro esta el clasificador)

---

## Paso 6: Validacion (Que tan bueno fue?)

Para saber si el clasificador funciona, comparamos su resultado con la **verdad de campo**
(pixeles donde un experto verifico la clase real):

```
                    PREDICCION
                 Urbano  Agua  Bosque
              +--------+------+-------+
Urbano        |   22   |   1  |   1   |  24
REAL   Agua   |    0   |  21  |   2   |  23
       Bosque |    1   |   2  |  20   |  23
              +--------+------+-------+
                 23       24     23      70

Aciertos (diagonal): 22 + 21 + 20 = 63

Overall Accuracy = 63 / 70 = 90.0%
```

**Kappa** mide la calidad descontando el azar (un clasificador aleatorio
tambien acertaria algunas veces):

```
Kappa = (Observado - Esperado) / (1 - Esperado)

Interpretacion:
  kappa > 0.80  = Excelente
  kappa 0.60-0.80 = Bueno
  kappa 0.40-0.60 = Moderado
  kappa < 0.40  = Pobre
```

En la tesis original: **OA = 81.87%, Kappa = 0.7637** (Bueno, casi Excelente).

---

## Resumen Visual del Proceso Completo

```
ENTRENAMIENTO                          CLASIFICACION
=============                          ==============

Experto selecciona                     Para cada pixel:
pixeles de ejemplo
     |                                 1. Leer valores de bandas
     v                                      |
Calcular media y                       2. Evaluar curvas de campana
desviacion estandar                    (una por clase por banda)
por clase por banda                         |
     |                                 3. Tomar el MINIMO por clase
     v                                 (la banda mas debil manda)
Construir curvas de                         |
campana (Gaussianas)                   4. La clase con el valor
     |                                 mas alto GANA
     v                                      |
28 curvas                              5. Guardar clase + confianza
(7 clases x 4 bandas)
                                            |
                                            v
                                       MAPA CLASIFICADO
                                       + Matriz de Confusion
                                       + Overall Accuracy
                                       + Kappa
```

---

## Por Que Logica Difusa?

A diferencia de otros metodos:

| Metodo | Como decide | Problema |
|:---|:---|:---|
| Distancia Minima | "A que clase esta mas cerca?" | Ignora la dispersion de los datos |
| Maxima Verosimilitud | "Cual distribucion es mas probable?" | Asume que los datos son normales |
| Arbol de Decision | "Si VNIR1 > 100 entonces..." | Limites rigidos, no graduales |
| **Logica Difusa** | **"Que tanto se parece a cada clase?"** | **Grados de pertenencia, no si/no** |

La logica difusa dice: "este pixel es **97% Urbano** y **2% Bosque**",
en vez de decir simplemente "es Urbano" o "no es Urbano". Esto es mas realista
porque en la naturaleza los limites entre clases son graduales, no abruptos.

---

## Opcion Avanzada: Clasificador Hibrido (ML.NET)

### El Problema que Resuelve

El clasificador difuso puro tiene una limitacion: la decision final es
simplemente "gana el que tenga el minimo mas alto". No aprende patrones
complejos entre las clases. Por ejemplo:

- Agricultura y Bosque pueden tener valores similares en 3 bandas pero
  diferir sutilmente en la cuarta
- El clasificador difuso puro no puede aprender que "si VNIR1 es medio-alto
  Y SWIR1 es bajo, probablemente es Bosque y no Agricultura"

La idea del hibrido es: **usar los grados de membresia difusos como entrada
para un algoritmo de Machine Learning** que SI puede aprender esos patrones.

### Como Funciona Paso a Paso

**Paso H1: Extraer features difusas de cada pixel**

Tomemos el mismo pixel de antes: `VNIR1=128, VNIR2=112, SWIR1=158, SWIR2=138`

El `FuzzyFeatureExtractor` genera un vector con TRES tipos de informacion:

```
TIPO 1 - Valores crudos (los mismos 4 numeros del pixel):

  VNIR1=128, VNIR2=112, SWIR1=158, SWIR2=138
  [128.0, 112.0, 158.0, 138.0]


TIPO 2 - Grados de membresia (cada curva de campana evaluada):
  Esto es lo que calculamos en el Paso 4a, pero GUARDAMOS todos los numeros.

  Urbano_VNIR1 = 0.99    Agua_VNIR1 = 0.00    Bosque_VNIR1 = 0.02
  Urbano_VNIR2 = 0.97    Agua_VNIR2 = 0.00    Bosque_VNIR2 = 0.17
  Urbano_SWIR1 = 0.99    Agua_SWIR1 = 0.00    Bosque_SWIR1 = 0.01
  Urbano_SWIR2 = 0.99    Agua_SWIR2 = 0.00    Bosque_SWIR2 = 0.00

  Con 7 clases x 4 bandas = 28 numeros
  [0.99, 0.97, 0.99, 0.99, 0.00, 0.00, 0.00, 0.00, 0.02, 0.17, 0.01, 0.00, ...]


TIPO 3 - Fuerzas de disparo (el minimo por clase, del Paso 4b):

  Fuerza_Urbano = 0.97
  Fuerza_Agua   = 0.00
  Fuerza_Bosque = 0.00
  ...
  Con 7 clases = 7 numeros
  [0.97, 0.00, 0.00, ...]
```

Todo junto forma **un vector de 39 numeros** (4 + 28 + 7) que describe
al pixel de una forma mucho mas rica que los 4 valores crudos originales.

**Paso H2: Entrenar un modelo ML con esos vectores**

Tomamos los mismos pixeles de entrenamiento del Paso 2 (donde el experto
dijo "este es Urbano", "este es Agua", etc.) y calculamos el vector de
39 features para cada uno:

```
Pixel de entrenamiento 1:
  Clase real: "Urbano"
  Vector: [125.0, 108.0, 155.0, 135.0, 0.98, 0.96, 0.98, 0.98, 0.00, ...]

Pixel de entrenamiento 2:
  Clase real: "Agua"
  Vector: [24.0, 13.0, 9.0, 7.0, 0.00, 0.00, 0.00, 0.00, 0.99, 0.98, ...]

Pixel de entrenamiento 3:
  Clase real: "Bosque"
  Vector: [76.0, 94.0, 83.0, 72.0, 0.01, 0.23, 0.01, 0.00, 0.00, ...]

... (50+ pixeles por clase)
```

Estos vectores + sus etiquetas se usan para entrenar un modelo ML.NET:

```
                50+ pixeles con etiqueta
                         |
                         v
             +------------------------+
             |    Random Forest       |
             |    (100 arboles de     |
             |     decision que       |
             |     votan juntos)      |
             +------------------------+
                         |
                         v
                  Modelo entrenado
                  (aprende reglas como:
                   "si Fuerza_Urbano > 0.8
                    Y Bosque_SWIR1 < 0.1
                    => es Urbano")
```

**Paso H3: Clasificar pixeles nuevos**

Para un pixel nuevo, el proceso es:

```
Pixel nuevo: VNIR1=128, VNIR2=112, SWIR1=158, SWIR2=138
                         |
                         v
              FuzzyFeatureExtractor
              (calcula los 39 numeros)
                         |
                         v
              [128, 112, 158, 138, 0.99, 0.97, ..., 0.97, 0.00, 0.00, ...]
                         |
                         v
              Modelo Random Forest
              (los 100 arboles votan)
                         |
                         v
                    "Urbano"
```

### Comparacion: Difuso Puro vs Hibrido

```
                        DIFUSO PURO              HIBRIDO (ML.NET)
                        -----------              ----------------
Entrada:                4 valores de pixel       39 features (crudos +
                                                  membresias + fuerzas)

Decision:               min() + argmax()         100 arboles de decision
                        (regla fija)             (aprende del dato)

Ventaja:                Simple, explicable,      Puede capturar patrones
                        no necesita muchos       complejos entre clases
                        datos de entrenamiento   que min/max no detecta

Limitacion:             Solo mira "que tan       Necesita mas datos de
                        cerca esta de cada       entrenamiento para
                        clase" por separado      aprender bien

Cuando usarlo:          Pocas muestras de        Muchas muestras de
                        entrenamiento,           entrenamiento,
                        clases bien separadas    clases que se solapan
```

### Ejemplo Real: Donde el Hibrido Gana

Imagina dos clases que se confunden: **Agricultura** y **Pastizal**.
Ambas tienen vegetacion, asi que sus valores espectrales son similares.

```
                       VNIR1   VNIR2   SWIR1   SWIR2
Agricultura tipica:      90      80      70      60
Pastizal tipico:         85      82      65      55

Diferencia:               5       2       5       5    (muy poca!)
```

El clasificador difuso puro calcula:

```
Fuerza_Agricultura = min(0.85, 0.88, 0.82, 0.84) = 0.82
Fuerza_Pastizal    = min(0.82, 0.90, 0.80, 0.82) = 0.80

Diferencia: solo 0.02!  Muchos pixeles se clasifican mal.
```

El Random Forest, en cambio, puede aprender que:
- "Si Fuerza_Agricultura Y Fuerza_Pastizal estan ambas arriba de 0.7,
  mirar el grado de membresia en SWIR1: si es > 0.83 para Agricultura,
  probablemente es Agricultura"

Esa **combinacion de multiples features** es algo que min/argmax no puede hacer
pero un arbol de decision si.

### K-Means: Sugerir Areas de Entrenamiento

Bonus: FuzzySat tambien incluye **K-Means clustering** que agrupa pixeles
similares sin necesidad de etiquetas. Esto ayuda al experto a identificar
donde hay grupos naturales en la imagen antes de etiquetar manualmente:

```
Imagen sin etiquetar          Despues de K-Means (k=5)
+---+---+---+---+             +---+---+---+---+
| ? | ? | ? | ? |             | 1 | 1 | 3 | 3 |
| ? | ? | ? | ? |   K-Means  | 1 | 2 | 3 | 4 |
| ? | ? | ? | ? |   ----->>  | 2 | 2 | 5 | 4 |
| ? | ? | ? | ? |             | 2 | 5 | 5 | 4 |
+---+---+---+---+             +---+---+---+---+

"Grupo 1 parece agua, Grupo 2 parece bosque..."
El experto luego verifica y etiqueta cada grupo.
```

---

*Documento generado para el proyecto FuzzySat. Para detalles tecnicos completos,
ver el [README principal](../README.md).*
