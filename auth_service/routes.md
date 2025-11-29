**EJEMPLO** 
// **get json de usuarios**
curl -X GET http://localhost:5002/auth/users      -H "Authorization: Bearer INSERTAR_TOKEN_ACTUAL"

RETORNA JSON CON DATOS DE USUARIOS
[{"id_usuario":1,"nombre_usuario":"usuario_nuevo_01","id_rol":1,"fecha_creacion":"2025-11-29T09:10:36.000Z"},{"id_usuario":3,"nombre_usuario":"usuario_nuevo_02","id_rol":2,"fecha_creacion":"2025-11-29T22:12:27.000Z"}]


// **POST REGISTER**
curl -X POST http://localhost:5002/auth/register      -H "Content-Type: application/json"      -d '{"nombre_usuario": "usuario_nuevo_03", "password": "mipassword234", "id_rol": 2}'

RETORNA MESSAGE JSON VALIDANDO LA TRANSACCION
{"message":"Usuario registrado exitosamente.","userId":5,"rolAsignado":2}

// **post LOGIN**
curl -X POST http://localhost:5002/auth/login      -H "Content-Type: application/json"      -d '{"nombre_usuario": "usuario_nuevo_01", "password": "mipassword123"}'

RETORNA (TOKEN ACTIVO)
{"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZF91c3VhcmlvIjoxLCJub21icmVfdXN1YXJpbyI6InVzdWFyaW9fbnVldm9fMDEiLCJpZF9yb2wiOjEsImlhdCI6MTc2NDQ1NzQ3OSwiZXhwIjoxNzY0NDYxMDc5fQ.xeDS0Yw4KJRoA7pJ8kJxCNHJttFiPZ7f5Oqpb3rKBEY"}eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZF91c3VhcmlvIjoxLCJub21icmVfdXN1YXJpbyI6InVzdWFyaW9fbnVldm9fMDEiLCJpZF9yb2wiOjEsImlhdCI6MTc2NDQ1NzQ3OSwiZXhwIjoxNzY0NDYxMDc5fQ.xeDS0Yw4KJRoA7pJ8kJxCNHJttFiPZ7f5Oqpb3rKBEY

// **PUT UPDATE ROLE**
// ACTUALIZA ROL DE USUARIO SEGUN ID
curl -X PUT http://localhost:5002/auth/update-role \
  -H "Authorization: Bearer INSERTAR_TOKEN_ACTUAL" \
  -H "Content-Type: application/json" \
  -d '{"id_usuario": 3, "new_id_rol": 1}'