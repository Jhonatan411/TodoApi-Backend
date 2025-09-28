#  API de Gestión de Pacientes

##  Descripción
Este proyecto es una **API RESTful en .NET 8** para la gestión de pacientes.  
Incluye CRUD completo, soporte de actualizaciones parciales (**PATCH**), filtros con paginación, y un **Stored Procedure** para consultar pacientes creados después de una fecha.  

También incluye **pruebas unitarias con xUnit** y usa **Entity Framework Core** con SQL Server como base de datos principal.

---

##  Tecnologías utilizadas
- .NET 8 Web API  
- Entity Framework Core (SQL Server + InMemory para pruebas)  
- xUnit (pruebas unitarias)  
- Swagger (documentación interactiva)  

---

##  Instalación y ejecución

### 1. Clonar el repositorio
    bash
    git clone https://github.com/tuusuario/TodoApi.git
    cd TodoApi

### 2. Configurar la cadena de conexión

En `appsettings.json`:

		```json
		"ConnectionStrings": {
		  "DefaultConnection": "Server=localhost;Database=PatientsDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
		}

##### Explicación  JSON

Server=localhost → servidor SQL (o localhost,1433 si usas Docker con puerto 1433).

Database=PatientsDb → nombre de la base de datos que EF creará/usarás.

User Id=sa;Password=YourPassword → credenciales (evita hardcodear: usa Secret Manager o variables de entorno).
(Cambia **YourPassword** por tu clave real de SQL Server.)

TrustServerCertificate=True → facilita desarrollo cuando hay certificado autofirmado; en producción usar certificado válido.


### 3. Crear la base de datos con migraciones

#### Requisitos

**Agregar paquetes al proyecto:**

		dotnet add package Microsoft.EntityFrameworkCore.SqlServer
		dotnet add package Microsoft.EntityFrameworkCore.Design


(Opcional si aún no lo tienes) instalar la herramienta EF CLI:

		dotnet tool install --global dotnet-ef
###### *o actualizar: dotnet tool update --global dotnet-ef*

#### Pasos

##### Crear la migración inicial:

		dotnet ef migrations add InitialCreate --project TodoApi --startup-project TodoApi

Esto crea archivos en TodoApi/Migrations/ con la instrucción CreateTable para Patients.

##### Aplicar migraciones (crear BD y tablas):

		dotnet ef database update --project TodoApi --startup-project TodoApi

EF se conectará usando DefaultConnection y ejecutará las instrucciones SQL.

#### Qué revisar después

- Verifica en SQL Server que exista la base *PatientsDb* y la tabla *Patients*.

- Revisa el archivo de migración: contiene *CreateTable*, índice único compuesto *(DocumentType, DocumentNumber).*

- **Si recibes errores:**

	-**Login failed:** credenciales incorrectas o SAS no habilitado.

	-**The server was not found:** servidor/puerto incorrecto o Docker no corriendo.

	-**Problemas con permisos:** usar un usuario con permisos suficientes para crear BD.

#### Notas sobre migraciones en equipo/CI

- Incluye las migraciones en el repositorio (no regenerarlas en cada máquina).

- En CI: ejecutar *dotnet ef database update* o ejecutar scripts SQL generados.

- Evita *EnsureCreated()* en aplicaciones que usan migraciones — se usan en pruebas ligeras.

### 4. Crear el Stored Procedure (detallado)
#### Script SQL

Ejecuta en tu servidor (SSMS / Azure Data Studio / sqlcmd):

	USE PatientsDb;
	GO
	CREATE OR ALTER PROCEDURE dbo.GetPatientsCreatedAfter
		@After DATETIME2
	AS
	BEGIN
		SET NOCOUNT ON;
		SELECT PatientId,
			   DocumentType,
			   DocumentNumber,
			   FirstName,
			   LastName,
			   BirthDate,
			   PhoneNumber,
			   Email,
			   CreatedAt
		FROM Patients
		WHERE CreatedAt > @After
		ORDER BY CreatedAt ASC;
	END
	GO

#### Por qué usar *CREATE OR ALTER* y *SET NOCOUNT ON* Por qué usar *CREATE OR ALTER* y *SET NOCOUNT ON*

- ***CREATE OR ALTER:*** permite ejecutar varias veces el script sin errores al rehacerlo.

- ***SET NOCOUNT ON:*** evita resultados adicionales (filas afectadas) que a veces interfieren en drivers.

#### Pruebas del SPPruebas del SP

- **Prueba manual:**

		EXEC dbo.GetPatientsCreatedAfter '2025-01-01'; 

- Verifica que los campos y tipos devueltos coinciden con tu entidad *Patient*.

#### Seguridad y permisos

- En producción, restringe permisos (ej. ejecutar SP a roles concretos).

- Evita concatenar SQL en el SP; usa parámetros (**@After**) — aquí es seguro.

### 5. Ejecutar la API (detallado)
#### Comando simple
		dotnet run --project TodoApi


- Muestra en consola líneas del tipo:

		Now listening on: http://localhost:5276
		Now listening on: https://localhost:7276


- Usa la URL mostrada.

#### HTTPS en desarrollo

- .NET genera un certificado dev. Si el navegador reclama, confía en el certificado de desarrollo o usa HTTP (configura Kestrel si quieres evitar HTTPS en local).

#### Cambiar puerto / launchSettings

- ***Properties/launchSettings.json*** controla puertos para dotnet run y Debug en VS Code.

- Para forzar puerto en ejecución:

		set ASPNETCORE_URLS=http://localhost:5276
		dotnet run --project TodoApi

#### Swagger UI

- Accede a http://localhost:5276/swagger/index.html para probar endpoints de forma interactiva y ver ejemplos.

### Endpoints disponibles

Para cada endpoint: ruta, método, body de ejemplo, validaciones, códigos HTTP y ejemplos de respuesta/error.

#### 1.  POST /api/patients — Crear paciente

- **Descripción:** Inserta un nuevo paciente.

- **Request headers:** Content-Type: application/json

- **Body (ejemplo)**:

		{
		  "documentType": "CC",
		  "documentNumber": "12345678",
		  "firstName": "Jhonatan",
		  "lastName": "Arrieta",
		  "birthDate": "1995-01-17",
		  "phoneNumber": "3001234567",
		  "email": "jhon@example.com"
		}


- **Validaciones:**

	- DocumentType requerido, max 10 chars.

	- DocumentNumber requerido, max 20 chars.

	- FirstName, LastName requeridos, max 80 chars.

	- BirthDate requerido.

	- Email formato válido si está presente.

- **Respuestas:**

	- 201 Created — cuerpo: PatientDto y header Location apuntando a /api/patients/{id}.

	- 400 Bad Request — validación falló (ModelState).

	- 409 Conflict — duplicado por (DocumentType, DocumentNumber).

-**Ejemplo de 201:**

		{
		  "patientId": 1,
		  "documentType": "CC",
		  "documentNumber": "12345678",
		  "firstName": "Jhonatan",
		  "lastName": "Arrieta",
		  "birthDate": "1995-01-17T00:00:00",
		  "phoneNumber": "3001234567",
		  "email": "jhon@example.com",
		  "createdAt": "2025-09-27T18:00:00Z"
		}

#### 2. GET /api/patients — Listado con filtros y paginación

- **Query params:**

	- page (int, default 1)

	- pageSize (int, default 10) — considera un maxPageSize en servidor (ej. 100).

	- name (string) — búsqueda contains sobre FirstName + " " + LastName.

	- documentNumber (string) — búsqueda exacta.

- **Comportamiento**:

	- Ordena por CreatedAt DESC.

	- Devuelve objeto con metadata: { total, page, pageSize, items: [...] }.

- **Respuestas**:

	-**200 OK**— items: array de PatientDto.

-**Ejemplo de llamada:**

		GET /api/patients?page=2&pageSize=5&name=Juan


**Consideraciones de diseño:**

- Implementar Skip((page-1)*pageSize).Take(pageSize).

- Evitar pageSize sin límite; imponer maxPageSize.

#### 3.  GET /api/patients/{id} — Obtener por id

- **Ruta: /api/patients/1**

-**Respuestas:**

- **200 OK**— PatientDto.

- **404 Not Found** — si no existe.

- **Nota:** Usar [HttpGet("{id:int}")] para evitar colisiones con rutas que no sean numéricas.

#### 4) PUT /api/patients/{id} — Reemplazo completo

- **Body:** Mismo esquema que PatientCreateDto.

- **Comportamiento:**

	- Reemplaza todos los campos.

	- Si se cambia documento, validar duplicado.

-**Respuestas:**

	- 204 No Content — éxito.

	- 400 Bad Request — validación.

	- 404 Not Found — id no existe.

	- 409 Conflict — otro paciente tiene ese documento.

#### 5) PATCH /api/patients/{id} — Actualización parcial

- **Content-Type:** application/json-patch+json

-**Body (ejemplo):**

		[
		  { "op": "replace", "path": "/firstName", "value": "Carlos" },
		  { "op": "replace", "path": "/email", "value": "carlos@example.com" }
		]


- **Implementación**:

	- Convertir entidad a **PatientUpdateDto**.

	- **patchDoc.ApplyTo(dto, ModelState).**

	- **TryValidateModel(dto).**

	- Validar duplicado si **DocumentType** o **DocumentNumber** cambiaron.

	- Mapear cambios a entidad y **SaveChanges**.

- **Respuestas**:

- **204 No Content** — éxito.

- **400 Bad Request** — patch inválido o ModelState inválido.

- **404 Not Found** — id no existe.

- **409 Conflict** — duplicado detectado.

#### 6) DELETE /api/patients/{id}

- **Respuestas:**

- **204 No Content** — eliminado.

- **404 Not Found** — id no existe.

#### Endpoint SP: GET /api/patients/created-after?after=YYYY-MM-DD

-**Query param**: after (fecha).

- **Implementación segura:**

		var patients = await _context.Patients
			.FromSqlInterpolated($"EXEC dbo.GetPatientsCreatedAfter {parsedDate}")
			.ToListAsync();

 **FromSqlInterpolated** aplica parametrización evitando inyección SQL.

- **Validación de fecha:**

	- Recibir string after, DateTime.TryParse → parsed.

	- Si inválido → 400 Bad Request con mensaje claro.

- **Respuesta:** **200 OK** con lista de **PatientDto**.

### Pruebas unitarias (minucioso)
#### Estructura del proyecto de pruebas

- **Proyecto**: **TodoApi.Tests** (xUnit) con referencia al proyecto TodoApi.

- **Paquetes**:

		dotnet add TodoApi.Tests package Microsoft.EntityFrameworkCore.InMemory
		dotnet add TodoApi.Tests package Microsoft.AspNetCore.Mvc


- **Patrón: Arrange / Act / Assert.**

#### Usar InMemory vs Sqlite

- **InMemoryDatabase**: rápido, fácil, ideal para pruebas unitarias que ejercitan la lógica del repositorio/DbContext.

- **Sqlite (in-memory):** más fiel al comportamiento de SQL real (p. ej. tipos, constraints), recomendable para tests de integración.

#### Ejemplo de Test para POST (creación)Ejemplo de Test para POST (creación)
		[Fact]
		public async Task CreatePatient_ReturnsCreated()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase("TestDb1")
				.Options;
			using var context = new AppDbContext(options);
			var controller = new PatientsController(context);

			var dto = new PatientCreateDto { DocumentType="CC", DocumentNumber="111", FirstName="Ana", LastName="Lopez", BirthDate=DateTime.Parse("1990-01-01") };

			var result = await controller.Create(dto);

			var created = Assert.IsType<CreatedAtActionResult>(result);
			var patient = Assert.IsType<PatientDto>(created.Value);
			Assert.Equal("111", patient.DocumentNumber);
		}

#### Ejemplo de Test para GET con filtro
		[Fact]
		public async Task GetPatients_FilterByName_ReturnsFiltered()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;
			using var context = new AppDbContext(options);
			context.Patients.AddRange(new Patient{ /* Juan */ }, new Patient{ /* Ana */});
			await context.SaveChangesAsync();

			var controller = new PatientsController(context);
			var result = await controller.Get(page:1, pageSize:10, name:"Juan", documentNumber:null);

			var ok = Assert.IsType<OkObjectResult>(result);
			var payload = ok.Value as dynamic;
			Assert.True(((IEnumerable<PatientDto>)payload.items).Any());
		}

#### Ejecutar tests
		dotnet test

- Integración CI

- En GitHub Actions: comando dotnet test en job de CI.

- Si usas migraciones en tests de integración, preparar SQL Server en job (docker service).

### Arquitectura (detallado) y responsabilidades
		TodoApi/
		│── Controllers/      → endpoints, validaciones por ModelState, respuestas HTTP
		│── DTOs/             → contratos de entrada/salida (PatientCreateDto, PatientDto, PatientUpdateDto)
		│── Entities/         → clases que representan tablas (Patient)
		│── Data/             → AppDbContext (configuración de modelos, índices, relaciones)
		│── Profiles/         → AutoMapper profiles (si se usa AutoMapper)
		│── Program.cs        → configuración DI, servicios, Swagger, middlewares
		│── Tests/            → proyecto de pruebas xUnit

- **Responsabilidades**

	- **Controllers**: orquestan peticiones, validan ModelState, llaman a services o DbContext.

	- **DTOs**: evitan exponer entidades directamente; lugar ideal para reglas de validación.

	- **Entities**: modelan la BD y contienen defaults (ej. CreatedAt).

	- **DbContext**: mapeo EF, índices, restricciones.

- **Diseño recomendado (si amplías)**

- Añadir capa Services para lógica de negocio y que Controllers solo llamen servicios.

- Añadir Repository si se necesita abstracción para testing o múltiples fuentes de datos.

### Diagrama (Mermaid)
		flowchart TD
			Client[Cliente/API Consumer] --> Controller[Controller: PatientsController]
			Controller --> DTOs[DTOs]
			Controller --> DbContext[AppDbContext]
			DbContext --> SQL[(SQL Server)]

### Decisiones técnicas — explicación y razones

- Entity Framework Core + SQL Server

	- Por qué: EF Core permite productividad con LINQ, migraciones y modelos fuertemente tipados.

	- Trade-offs: EF añade una capa de abstracción (posible overhead). Si requieres máximo rendimiento en consultas complejas, se pueden usar Dapper o consultas SQL optimizadas.

- Índice/Restricción única (DocumentType, DocumentNumber)

	- Por qué: evita duplicados lógicos. Mantener la integridad en BD es más seguro que solo validarlo a nivel aplicación.

	- Nota: además validar en la API para devolver 409 Conflict amigable.

- DTOs (separación de entidades)

	- Por qué: desacopla modelo de persistencia del contrato público. Facilita evolutividad y seguridad (oculta campos internos).

- PATCH con JsonPatchDocument

	- Por qué: permite actualizaciones parciales sin enviar objeto completo. Es estándar (RFC 6902).

	- Consideración: requiere AddNewtonsoftJson() y Content-Type: application/json-patch+json.

- Stored Procedure

	- Por qué: mostrar integración con SP; buena para lógica de consulta compleja, auditoría o performance en ciertos escenarios.

	- Seguridad: usar parámetros y evitar concatenación.

- xUnit

	- Por qué: estándar en ecosistema .NET, integración simple con dotnet test y runners CI.

- Swagger

	- Por qué: documentación interactiva y utilidad para evaluadores de pruebas técnicas.

	- Manejo de errores

	- Implementar middleware global que transforme excepciones en ProblemDetails (RFC 7807).

	- Devolver mensajes claros y códigos HTTP adecuados (400, 404, 409, 500).

- Seguridad

	- No exponer credenciales.

	- Preparar para autenticación (JWT) si se requiere en fases futuras.

	- Validar inputs y usar FromSqlInterpolated para protección contra SQL injection.

- Despliegue / Docker

	- Recomiendo Docker Compose con servicios api y mssql para evaluación: facilita reproducibilidad del entorno del evaluador.
