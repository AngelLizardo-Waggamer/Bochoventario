import { useState, useEffect } from 'react';
import { Table, Button, Container, Navbar, Nav, Card, Spinner, Alert, Modal, Form } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import { inventoryService } from '../services/api';

function Inventory() {
  const navigate = useNavigate();
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [showModal, setShowModal] = useState(false);
  const [newProduct, setNewProduct] = useState({
    sku: '', name: '', description: '', price: ''
  });

  // CARGAR DATOS
  useEffect(() => {
    fetchProducts();
  }, []);

  const fetchProducts = async () => {
    try {
      const data = await inventoryService.getAll();
      setProducts(data);
      setLoading(false);
    } catch (err) {
      console.error(err);
      setError("No se pudo conectar con el servidor. Revisa que el Backend esté corriendo.");
      setLoading(false);
    }
  };

  // MANEJO DEL MODAL
  const handleShow = () => setShowModal(true);
  const handleClose = () => setShowModal(false);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setNewProduct({ ...newProduct, [name]: value });
  };

  const handleCreateProduct = async (e) => {
    e.preventDefault();
    try {
      await inventoryService.create(newProduct);
      
      // Si todo sale bien:
      alert("¡Producto agregado exitosamente!");
      handleClose();
      fetchProducts();
      setNewProduct({ sku: '', name: '', description: '', price: '' });
    } catch (err) {
      alert("Error al crear: " + err.message);
    }
  };

  const handleDelete = async (id) => {
    if(!window.confirm("¿Eliminar producto?")) return;
    try {
      await inventoryService.delete(id);
      fetchProducts();
    } catch (err) {
      alert("Error al eliminar: " + err.message);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    navigate('/login');
  };

  return (
    <>
      <Navbar bg="dark" variant="dark" expand="lg" className="mb-4">
        <Container>
          <Navbar.Brand href="#home">El Bochoventario</Navbar.Brand>
          <Nav className="ms-auto">
             <Button variant="warning" className="me-2" onClick={() => navigate('/admin')}>
               Panel admin
             </Button>
            <Button variant="outline-light" onClick={handleLogout}>Cerrar sesión</Button>
          </Nav>
        </Container>
      </Navbar>

      <Container>
        <div className="d-flex justify-content-between align-items-center mb-4">
          <h2>Inventario actual</h2>
          <Button variant="primary" onClick={handleShow}>+ Agregar producto</Button>
        </div>

        {error && <Alert variant="danger">{error}</Alert>}

        <Card className="shadow-sm border-0">
            {loading ? (
                <div className="text-center p-5">
                    <Spinner animation="border" variant="primary" />
                </div>
            ) : (
                <Table striped hover responsive className="mb-0">
                <thead className="bg-light">
                    <tr>
                    <th>SKU</th>
                    <th>Producto</th>
                    <th>Descripción</th>
                    <th>Precio</th>
                    <th>Stock</th>
                    <th>Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    {products.length === 0 ? (
                        <tr><td colSpan="6" className="text-center">No hay productos. ¡Agrega uno!</td></tr>
                    ) : (
                        products.map((p) => (
                        <tr key={p.id}>
                            <td>{p.sku || 'N/A'}</td>
                            <td>{p.name}</td>
                            <td><small className="text-muted">{p.description}</small></td>
                            <td>${p.price}</td>
                            <td>{p.quantity}</td> {/* Ahorita saldrá 0 siempre */}
                            <td>
                            <Button variant="danger" size="sm" onClick={() => handleDelete(p.id)}>Eliminar</Button>
                            </td>
                        </tr>
                        ))
                    )}
                </tbody>
                </Table>
            )}
        </Card>
      </Container>

      {/* MODAL PARA AGREGAR PRODUCTO */}
      <Modal show={showModal} onHide={handleClose}>
        <Modal.Header closeButton>
          <Modal.Title>Nuevo Producto</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <Form onSubmit={handleCreateProduct}>
            <Form.Group className="mb-3">
              <Form.Label>SKU (Código)</Form.Label>
              <Form.Control name="sku" required onChange={handleInputChange} />
            </Form.Group>
            <Form.Group className="mb-3">
              <Form.Label>Nombre</Form.Label>
              <Form.Control name="name" required onChange={handleInputChange} />
            </Form.Group>
            <Form.Group className="mb-3">
              <Form.Label>Descripción</Form.Label>
              <Form.Control name="description" as="textarea" rows={2} onChange={handleInputChange} />
            </Form.Group>
            <Form.Group className="mb-3">
              <Form.Label>Precio Costo</Form.Label>
              <Form.Control type="number" name="price" required onChange={handleInputChange} />
            </Form.Group>
            
            <div className="d-flex justify-content-end gap-2">
                <Button variant="secondary" onClick={handleClose}>Cancelar</Button>
                <Button variant="primary" type="submit">Guardar</Button>
            </div>
          </Form>
        </Modal.Body>
      </Modal>
    </>
  );
}

export default Inventory;