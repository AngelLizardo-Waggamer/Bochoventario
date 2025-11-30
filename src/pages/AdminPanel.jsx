import { useState } from 'react';
import { Table, Form, Button, Container, Badge, Navbar, Nav, Card } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';

function AdminPanel() {
  const navigate = useNavigate();
  // Datos de prueba de usuarios con mis cabras
  const [users, setUsers] = useState([
    { id: 1, email: 'fuabl@bocho.com', role: 'admin' },
    { id: 2, email: 'angel@bocho.com', role: 'gestor' },
    { id: 3, email: 'baussy@bocho.com', role: 'lector' },
  ]);

  const handleRoleChange = (userId, newRole) => {
    // Aquí conectar con el backend para guardar el cambio
    console.log(`Usuario ${userId} cambiado a ${newRole}`);
    setUsers(users.map(u => u.id === userId ? { ...u, role: newRole } : u));
  };

  return (
    <>
      <Navbar bg="dark" variant="dark" className="mb-4">
        <Container>
          <Navbar.Brand>Panel de administración</Navbar.Brand>
          <Nav className="ms-auto">
            <Button variant="outline-light" onClick={() => navigate('/inventory')}>
              Volver al inventario
            </Button>
          </Nav>
        </Container>
      </Navbar>

      <Container>
        <Card className="shadow-sm border-0">
          <Card.Body>
            <h3 className="mb-4">Gestión de usuarios</h3>
            <Table striped hover responsive>
              <thead>
                <tr>
                  <th>Email</th>
                  <th>Rol actual</th>
                  <th>Asignar rol</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td className="align-middle">{user.email}</td>
                    <td className="align-middle">
                      <Badge bg={
                        user.role === 'admin' ? 'danger' : 
                        user.role === 'gestor' ? 'primary' : 'secondary'
                      }>
                        {user.role.toUpperCase()}
                      </Badge>
                    </td>
                    <td>
                      <Form.Select 
                        value={user.role}
                        onChange={(e) => handleRoleChange(user.id, e.target.value)}
                        size="sm"
                      >
                        <option value="lector">Lector</option>
                        <option value="gestor">Gestor</option>
                        <option value="admin">Admin</option>
                      </Form.Select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </Card.Body>
        </Card>
      </Container>
    </>
  );
}

export default AdminPanel;