import { useState } from 'react';
import { Form, Button, Card, Container } from 'react-bootstrap';
import { Link, useNavigate } from 'react-router-dom';

function Register() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const navigate = useNavigate();

  const handleSubmit = (e) => {
    e.preventDefault();
    if(password !== confirmPassword) {
        alert("Las contraseñas no coinciden");
        return;
    }
    // Aquí iría la conexión al backend
    navigate('/login');
  };

  return (
    <Container 
      fluid 
      className="d-flex justify-content-center align-items-center bg-light" 
      style={{ minHeight: '100vh' }}
    >
      <Card style={{ width: '400px' }} className="shadow border-0">
        <Card.Body className="p-5">
          <h2 className="text-center mb-4 fw-bold">Crear cuenta</h2>
          
          <Form onSubmit={handleSubmit}>
            <Form.Group className="mb-3">
              <Form.Label>Correo electrónico</Form.Label>
              <Form.Control 
                type="email" 
                placeholder="nombre@ejemplo.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                className="py-2"
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Contraseña</Form.Label>
              <Form.Control 
                type="password" 
                placeholder="******"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="py-2"
              />
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Label>Confirmar contraseña</Form.Label>
              <Form.Control 
                type="password" 
                placeholder="******"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
                className="py-2"
              />
            </Form.Group>

            <Button className="w-100 py-2 fw-bold" type="submit" variant="success">
              Registrarse
            </Button>
          </Form>

          <div className="w-100 text-center mt-4">
            ¿Ya tienes cuenta? <Link to="/login" className="text-decoration-none">Inicia sesión</Link>
          </div>
        </Card.Body>
      </Card>
    </Container>
  );
}

export default Register;