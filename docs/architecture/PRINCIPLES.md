# PRINCIPLES

El desarrollo, mantenimiento y evolución de OCAP se rige por los siguientes principios rectores:

## 1. Open Source First
Todo el código núcleo y la arquitectura base siempre será Open Source. Promovemos las contribuciones, la revisión pública del código y el desarrollo impulsado por la comunidad para garantizar transparencia y longevidad.

## 2. Self Hosted First
La plataforma está diseñada asumiendo que el usuario final la alojará en su propia infraestructura. No hay dependencias centrales que comprometan la soberanía de los datos.

## 3. API First
Todas las funcionalidades, desde la administración básica hasta la operación compleja, deben estar disponibles a través de una API. El Dashboard es solo un cliente más de la plataforma.

## 4. Security by Design
Asumimos que OCAP operará en entornos empresariales. La seguridad, el manejo adecuado de secretos, la auditoría de accesos y la protección contra vectores de ataque comunes son consideraciones primarias desde el momento cero del diseño.

## 5. Provider Agnostic
El dominio de OCAP nunca debe acoplarse a un proveedor de servicios externo. Si usamos OpenAI, debe ser a través de un puerto genérico de IA. Si usamos Google Calendar, debe ser a través de un puerto genérico de calendarios.

## 6. Channel Agnostic
Las reglas conversacionales no pueden depender de las características específicas de un canal. Un flujo de automatización debe ser capaz de ejecutarse idénticamente ya sea si el usuario se comunica por Telegram, WhatsApp o un WebChat.

## 7. Cloud Ready
Aunque priorizamos el Self-Hosting, la arquitectura (manejo de estado, configuración, almacenamiento) debe estar preparada para desplegarse fácilmente en entornos Cloud nativos (AWS, Google Cloud, Azure).

## 8. Docker First
Docker es la tecnología estándar de empaquetado y distribución. Cualquier componente del sistema debe ser contenerizable y orquestable mediante Docker y Docker Compose, facilitando la instalación en cualquier sistema operativo.
