# Terra ML: Script para NotebookLM

> Guion para generar un episodio tipo podcast con Google NotebookLM.
> Tono: profesional, tecnico pero accesible, honesto sobre limitaciones, seguro sobre lo construido.
> Duracion estimada: 18-22 minutos.

---

## CONTEXTO PARA NOTEBOOKLM

Este documento cuenta la historia de Terra ML, una plataforma open source para clasificacion
de imagenes satelitales que combina logica difusa, machine learning hibrido, y modelos de ML
puros. El proyecto nacio de una tesis de grado de 2008 en la Universidad de Los Andes, Venezuela,
pero ha evolucionado mucho mas alla de aquella tesis original.

La narrativa es honesta y profesional. La tesis fue la chispa: un clasificador difuso que
alcanzo 81.87% de precision con herramientas propietarias. Hoy, Terra ML es una plataforma
completa con 6 clasificadores de ML, metodos de ensemble, validacion cruzada, una aplicacion
web, soporte Docker, y 484 pruebas unitarias. Todo open source, todo gratuito.

Esta es esa historia.

---

## PARTE 1: LA CHISPA (El Origen)

En noviembre de 2008, un estudiante de ingenieria en la Universidad de Los Andes, en Merida,
Venezuela, presento una tesis: "Desarrollo de un Clasificador de Imagenes Satelitales Basado
en Logica Difusa".

La idea era tomar imagenes satelitales multiespectrales, donde cada pixel tiene valores de
reflectancia en multiples bandas de luz, y clasificar automaticamente la cobertura del terreno:
agua, bosque, zona urbana, agricultura, suelo desnudo. No con limites rigidos sino con grados
de pertenencia. Logica difusa.

El resultado fue 81.87% de precision general con un coeficiente Kappa de 0.7637, superando
los tres metodos clasicos de la epoca:

- Maxima Verosimilitud: 74.27%
- Arbol de Decision: 63.74%
- Distancia Minima: 56.14%

Pero todo estaba construido sobre MATLAB e IDRISI, herramientas con licencias que cuestan
miles de dolares. La tesis fue defendida, el diploma entregado, y 105 paginas de algoritmos
quedaron archivadas. Nadie podia replicar el trabajo sin comprar el mismo software costoso.

18 anos despues, el autor decidio volver a ese trabajo. No por nostalgia, sino porque el
mundo habia cambiado: imagenes satelitales gratuitas con Sentinel-2 de Copernicus, herramientas
open source como GDAL y ML.NET, y GitHub para compartir codigo con el mundo.

Esa tesis fue la chispa. Lo que existe hoy es algo mucho mas grande.

---

## PARTE 2: QUE ES TERRA ML HOY (La Plataforma)

Terra ML es una plataforma open source de clasificacion de imagenes satelitales construida
en C# y .NET 10. No es una reimplementacion de una tesis. Es un proyecto nuevo, profesional,
inspirado en aquella tesis pero que va mucho mas alla.

La plataforma ofrece tres modos de clasificacion:

El primero es logica difusa pura. El metodo original de la tesis: funciones de membresia,
inferencia difusa, defuzzificacion. Completamente explicable, cada decision se puede rastrear
hasta grados de membresia especificos en bandas espectrales especificas.

El segundo es el modo hibrido. Aqui la logica difusa se convierte en un preprocesador
inteligente que enriquece los datos antes de pasarlos a un modelo de machine learning. En
vez de darle al modelo 4 numeros crudos por pixel, le da 39 numeros con contexto semantico:
valores originales, grados de membresia, y fuerzas de disparo.

El tercero es ML puro. Los valores espectrales crudos van directamente a un clasificador
de machine learning, sin pasar por logica difusa. Para comparar o para datasets donde el
preprocesamiento difuso no aporta mejora.

Y no es solo un clasificador. Terra ML incluye:

- 6 clasificadores de machine learning: Random Forest, SDCA, LightGBM, SVM, Regresion
  Logistica, y una Red Neuronal MLP implementada con TorchSharp
- 2 metodos de ensemble: votacion por mayoria o ponderada, y stacking con meta-learner
- Un motor de comparacion de modelos con validacion cruzada k-fold que permite evaluar
  todos los metodos lado a lado
- 4 tipos de funciones de membresia: Gaussiana, Triangular, Trapezoidal, y Bell Generalizada
- Una aplicacion web completa en Blazor con 7 paginas que guian al usuario desde la
  configuracion del proyecto hasta la comparacion de modelos
- Una CLI con 5 comandos para automatizacion
- Soporte Docker para despliegue sin instalar nada
- 484 pruebas unitarias que validan la correccion matematica de cada componente

Esto no compite con ArcGIS ni con Google Earth Engine. Pero es una plataforma seria, open
source, que pone herramientas de clasificacion satelital profesionales al alcance de cualquiera
con una conexion a internet.

---

## PARTE 3: COMO FUNCIONA LA LOGICA DIFUSA (Los Fundamentos)

Vamos a explicar el concepto central de la forma mas simple posible, porque entender la
logica difusa es entender el corazon de Terra ML.

Imagina que tienes una imagen satelital. No es una foto normal. Es una imagen con 4, 10,
o hasta 13 capas, cada una capturando un tipo diferente de luz: visible, infrarrojo cercano,
infrarrojo de onda corta. Cada pixel tiene un numero por cada capa.

Cada material tiene su "firma espectral". El agua refleja poca luz en casi todas las bandas.
El bosque refleja mucho en infrarrojo cercano. Las zonas urbanas tienen una firma distintiva
en infrarrojo de onda corta.

El enfoque clasico dice: "Si el infrarrojo es mayor a 100, es bosque." Limites rigidos.
La logica difusa dice: "Este pixel se parece 97% a zona urbana, 2% a bosque, y 0% a agua."
Grados de pertenencia. Y eso es mas realista porque en la naturaleza las transiciones entre
coberturas son graduales, no lineas perfectas.

El proceso tiene cuatro pasos:

Primero, ENTRENAMIENTO. Un experto senala pixeles donde sabe que hay agua, bosque, zona
urbana. Terra ML calcula la media y la desviacion estandar de cada clase en cada banda.

Segundo, FUZZIFICACION. Con esas estadisticas se construyen curvas de campana gaussiana.
Cuando llega un pixel nuevo, se evalua contra todas las curvas y se obtiene un grado de
membresia entre 0 y 1 para cada clase en cada banda.

Tercero, INFERENCIA. Para cada clase, se toma el minimo de todos los grados de membresia
en todas las bandas. Es como decir: "Solo te considero urbano si te pareces a urbano en
TODAS las bandas." La banda mas debil manda.

Cuarto, DEFUZZIFICACION. La clase con el valor mas alto gana. Si "Urbano" tiene 0.97 y
"Bosque" tiene 0.02, el pixel es Urbano con 97% de confianza.

Esto se repite para cada pixel de la imagen. El resultado es un mapa tematico donde cada
pixel tiene una clase y un nivel de confianza. Y lo mas importante: si alguien pregunta
por que un pixel se clasifico como bosque, se puede rastrear exactamente cual curva en
cual banda produjo cual grado de membresia.

---

## PARTE 4: TRES CAMINOS PARA CLASIFICAR (La Evolucion)

Aqui es donde la historia se pone interesante. Porque Terra ML no se quedo con un solo
metodo. Ofrece tres caminos, y la razon de cada uno tiene que ver con una evolucion honesta
del proyecto.

El primer camino es la logica difusa pura. El metodo original. Funciona con muy pocos datos
de entrenamiento, es completamente explicable, y produce resultados razonables. Pero tiene
una limitacion fundamental: su regla de decision es fija. Toma el minimo de membresia entre
bandas y elige el maximo. Elegante, pero no puede aprender patrones complejos.

Cuando dos clases tienen firmas espectrales muy similares, como Agricultura y Pastizal,
las fuerzas de disparo pueden diferir por solo 0.02. A ese margen, el ruido del sensor
puede invertir la clasificacion. Y la logica difusa no tiene mecanismo para resolver eso
porque min-luego-max es la unica regla que conoce.

El segundo camino es el modo hibrido. Y aqui es donde la logica difusa encuentra su mejor
version: no como clasificador final, sino como preprocesador inteligente.

La pregunta que muchos hacen es: si usamos machine learning, para que necesitamos la logica
difusa? Podemos tirar los valores crudos directamente al modelo y ya.

Se puede hacer eso. Pero el resultado suele ser inferior. La razon es esta: un pixel con
4 bandas le da al modelo 4 numeros sin contexto. Pero si primero pasa por el motor difuso,
se obtienen 39 numeros con contexto semantico:

- Los 4 valores crudos originales
- 28 grados de membresia: cuanto se parece el pixel a cada clase en cada banda
- 7 fuerzas de disparo: el resultado de la inferencia difusa para cada clase

Es la diferencia entre darle a un medico solo los numeros de un analisis de sangre versus
darle los numeros mas la interpretacion de cada valor. Con la interpretacion incluida, toma
mejores decisiones.

Y ahora, el modelo de machine learning no tiene que descubrir desde cero que el valor 130
en VNIR1 es "tipico de Urbano". Ya lo sabe porque la logica difusa se lo dice: "Este pixel
tiene un grado de membresia de 0.99 en Urbano para VNIR1."

Terra ML ofrece 6 clasificadores para el modo hibrido: Random Forest con 100 arboles de
decision, SDCA MaximumEntropy para datasets grandes, LightGBM para alta precision con
gradient boosting, SVM con estrategia One-vs-All, Regresion Logistica L-BFGS con
probabilidades calibradas, y una Red Neuronal MLP implementada con TorchSharp que incluye
batch normalization, dropout, y early stopping.

El tercer camino es ML puro. Aqui se usa el RawFeatureExtractor que toma solamente las
bandas espectrales crudas y las pasa directamente al clasificador. Sin logica difusa de
por medio. Esto sirve como linea base para comparar, o para datasets donde el
preprocesamiento difuso no aporta mejora.

Terra ML no obliga a elegir un enfoque. Ofrece las herramientas para comparar y decidir
basado en datos.

---

## PARTE 5: ENSEMBLE Y COMPARACION DE MODELOS (Ingenieria Seria)

Cuando tienes 6 clasificadores y 2 modos de features, la pregunta natural es: cual es
mejor para mi dataset? Terra ML responde eso con ingenieria seria.

Primero, los metodos de ensemble. En vez de depender de un solo clasificador, puedes
combinar varios:

El EnsembleClassifier implementa votacion: cada clasificador emite su prediccion y gana
la clase con mas votos. Puede ser votacion por mayoria simple o votacion ponderada donde
clasificadores con mejor rendimiento tienen mas peso.

El StackingClassifier va un paso mas alla. Entrena multiples clasificadores base en
subconjuntos k-fold del dataset. Luego entrena un meta-learner, una Regresion Logistica,
sobre las predicciones out-of-fold de los clasificadores base. El meta-learner aprende
cual clasificador es mas confiable en que situaciones. Y se evita data leakage usando
estratified k-fold.

Segundo, la comparacion de modelos. El ModelComparisonEngine ejecuta validacion cruzada
k-fold para 5 clasificadores principales, tanto en modo hibrido como en ML puro, y produce
un ranking por Overall Accuracy y coeficiente Kappa. Esto permite ver, con datos concretos,
que metodo funciona mejor para un dataset especifico.

La Red Neuronal MLP esta disponible para clasificacion pero se excluye de la validacion
cruzada repetida por su tiempo de entrenamiento.

Con estos resultados en mano, se puede tomar una decision informada: logica difusa pura
para explicabilidad, hibrido para precision enriquecida, ML puro como linea base, o
ensemble para combinar lo mejor de varios metodos.

---

## PARTE 6: BAJO EL CAPO (La Arquitectura)

Terra ML esta construido en C# 13 sobre .NET 10 con una arquitectura limpia en capas.

El nucleo es FuzzySat.Core, una libreria que contiene todo el motor de clasificacion.
No depende de interfaces de usuario ni de formatos de archivo. Es pura logica.

Dentro del Core hay varios modulos:

El modulo de Funciones de Membresia implementa cuatro tipos de curvas: Gaussiana, la
original de la tesis, Triangular para limites precisos, Trapezoidal para rangos amplios,
y Bell Generalizada con pendiente ajustable. Todas implementan la misma interfaz y se
pueden intercambiar.

El motor de inferencia evalua todas las reglas difusas con operadores AND configurables:
minimo o producto algebraico. Dos defuzzificadores: Max Weight y Weighted Average.

El modulo ML contiene los 6 clasificadores, 2 extractores de features (FuzzyFeatureExtractor
y RawFeatureExtractor), el EnsembleClassifier, el StackingClassifier, el CrossValidator,
el ModelComparisonEngine, y el KMeansClusterer para sugerencia no supervisada de areas de
entrenamiento.

El modulo de Validacion calcula la matriz de confusion, la precision general, el coeficiente
Kappa, y las precisiones del productor y del usuario por clase.

El modulo de Raster usa GDAL para leer y escribir imagenes GeoTIFF con metadatos
geoespaciales, e incluye calculadores de indices espectrales: NDVI, NDWI, NDBI.

Sobre ese nucleo se construyen tres interfaces:

Una CLI con 5 comandos: classify, train, validate, info, y visualize. Construida con
System.CommandLine y Spectre.Console.

Una aplicacion web en Blazor Server con 7 paginas: Home, Project Setup, Training,
Classification, Validation, Model Comparison, e History. Construida con componentes Radzen.

Y una API REST para integracion con otros sistemas.

El despliegue es flexible: puede correr directamente con dotnet run, o en Docker con un
docker-compose up que levanta todo sin instalar SDK ni GDAL.

Todo esta respaldado por 484 pruebas unitarias: 349 en Core, 119 en Web, 16 en CLI. No
son solo pruebas de compilacion. Son pruebas de correccion matematica. Un test verifica
que la funcion Gaussiana a un sigma de distancia produce exactamente exp(-0.5). Otro
construye una matriz de confusion de 7 clases con 171 muestras y verifica 81.87% de
precision. Otro entrena un Random Forest con datos sinteticos y confirma clasificacion
correcta. Otros crean archivos GeoTIFF reales con GDAL, los escriben, los leen, y
verifican los valores pixel por pixel.

---

## PARTE 7: REFLEXIONES HONESTAS (Lo Que Se Ha Aprendido)

Hay algo importante que decir aqui, porque los buenos proyectos son honestos sobre lo que
pueden y lo que no pueden hacer.

La logica difusa pura funciono bien en 2008. Un 81.87% de precision no es poca cosa,
especialmente superando a los otros tres metodos de la epoca. Pero la clasificacion de
imagenes ha avanzado enormemente desde entonces.

Lo que se ha observado durante el desarrollo de Terra ML es que los enfoques de machine
learning, especialmente cuando se alimentan con features enriquecidas por logica difusa,
tienden a producir mejores resultados. Hay mas pruebas por hacer y mas conclusiones por
sacar, pero la tendencia es clara.

Dicho esto, cada modo tiene su lugar:

La logica difusa pura ofrece explicabilidad total. Cada decision se puede rastrear. Necesita
muy pocos datos de entrenamiento. Es excelente para educacion y para entender los
fundamentos de la clasificacion espectral.

El modo hibrido combina lo mejor de ambos mundos: la comprension fisica de la logica difusa
con la capacidad de aprendizaje del machine learning.

El modo ML puro sirve como linea base y funciona bien cuando la relacion entre bandas
espectrales y clases es mas directa.

Y los metodos de ensemble permiten combinar multiples perspectivas para mayor robustez.

Terra ML no compite con ArcGIS, IDRISI, ni Google Earth Engine. Son herramientas con
decadas de desarrollo y equipos enormes. Pero Terra ML es una aproximacion seria, open
source, gratuita, que pone herramientas de clasificacion satelital profesionales al alcance
de estudiantes, investigadores, y la comunidad de geomatica.

---

## PARTE 8: EL CAMINO ADELANTE (El Futuro)

Lo que comenzo como una tesis archivada se ha convertido en una plataforma de clasificacion
completa. El motor de logica difusa funciona. Los 6 clasificadores de ML estan integrados.
Los metodos de ensemble estan operativos. La aplicacion web guia al usuario paso a paso.
La validacion cruzada permite comparar metodos objetivamente.

El siguiente paso es mas validacion real con imagenes Sentinel-2 de Copernicus. Definir
areas de entrenamiento, clasificar, medir precision, comparar todos los metodos lado a
lado, y publicar los resultados, sean cuales sean.

La vision es que Terra ML sirva como puente:

Para estudiantes de teledeteccion que quieran entender la clasificacion desde los
fundamentos matematicos, con codigo que pueden leer, modificar, y ejecutar.

Para investigadores que necesiten modelos explicables donde cada decision se pueda rastrear
hasta grados de membresia especificos en bandas especificas.

Para la comunidad open source que trabaja con imagenes satelitales y quiera una alternativa
gratuita a las soluciones propietarias.

Y para cualquiera que quiera experimentar con clasificacion satelital sin barreras de
entrada: sin licencias, sin costos, con imagenes gratuitas de Copernicus, y con Docker
para levantar todo en un comando.

El codigo esta en GitHub, la licencia es MIT, y las contribuciones son bienvenidas.

De una tesis de 2008 a una plataforma moderna. De 2 clasificadores a 6, mas ensembles.
De 233 pruebas a 484. De logica difusa sola a tres modos de clasificacion.

La tesis abrio una puerta. Terra ML la cruzo.

---

## DATOS PARA REFERENCIA DE NOTEBOOKLM

**Proyecto**: Terra ML
**Repositorio**: github.com/ivanrlg/TerraML
**Autor**: Ivan R. Labrador Gonzalez
**Blog**: ivansingleton.dev
**Tesis original**: Universidad de Los Andes, Merida, Venezuela, Noviembre 2008
**Licencia**: MIT (open source)

**Tecnologias**: C# 13, .NET 10, GDAL 3.12, ML.NET 5.0, TorchSharp, Blazor Server, Radzen, xUnit
**Tests**: 484 pruebas unitarias (349 Core + 119 Web + 16 CLI)
**Docker**: Si, multi-stage build con docker-compose

**Modos de clasificacion**: Logica Difusa Pura, Hibrido (Difusa+ML), ML Puro
**Clasificadores ML**: Random Forest, SDCA, LightGBM, SVM, Regresion Logistica, MLP Neural Network
**Ensemble**: Votacion (mayoria/ponderada), Stacking con meta-learner
**Extractores de features**: FuzzyFeatureExtractor (hibrido), RawFeatureExtractor (ML puro)
**Validacion**: CrossValidator (k-fold), ModelComparisonEngine

**Funciones de membresia**: Gaussiana, Triangular, Trapezoidal, Bell Generalizada
**Operadores AND**: Minimo (tesis), Producto (alternativo)
**Defuzzificadores**: Max Weight (tesis), Weighted Average (alternativo)
**Indices espectrales**: NDVI, NDWI, NDBI
**Clustering**: K-Means para sugerencia de areas de entrenamiento

**Precision original (tesis 2008)**:
- Logica Difusa: 81.87% OA, Kappa 0.7637 (ganador)
- Maxima Verosimilitud: 74.27% OA, Kappa 0.6650
- Arbol de Decision: 63.74% OA, Kappa 0.5312
- Distancia Minima: 56.14% OA, Kappa 0.4233

**Imagenes satelitales**:
- ASTER (2008): 4 bandas VNIR/SWIR, 15m resolucion
- Sentinel-2 (2026): 13 bandas, 10-60m resolucion, gratuitas via Copernicus
- Landsat 8/9: 11 bandas, 15-100m, gratuitas via USGS
- Custom GeoTIFF: cualquier imagen multibanda

**Aplicacion web**: 7 paginas (Home, Project Setup, Training, Classification, Validation, Model Comparison, History)
**CLI**: 5 comandos (classify, train, validate, info, visualize)

**Formula Gaussiana**: mu(x) = exp(-0.5 * ((x - media) / desv)^2)
**Formula Kappa**: kappa = (Po - Pe) / (1 - Pe)
**Formula NDVI**: (NIR - Red) / (NIR + Red)
**Vector de features (hibrido)**: N_bands + N_classes x (N_bands + 1)
  - 4 bandas, 7 clases = 39 features
  - 13 bandas, 7 clases = 111 features

**Ganchos narrativos para el podcast**:
- "La tesis fue la chispa. Lo que existe hoy es algo mucho mas grande."
- "Tres caminos para clasificar: difusa, hibrida, ML puro. Tu eliges."
- "4 numeros sin contexto versus 39 numeros con significado semantico."
- "La analogia del medico: numeros mas interpretacion."
- "No compite con ArcGIS. Pero es open source, gratuito, y serio."
- "484 pruebas que verifican correccion matematica, no solo compilacion."
- "La tesis abrio una puerta. Terra ML la cruzo."
