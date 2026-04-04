const express = require('express');
const app = express();
app.use(express.json({ limit: '50mb' }));

const PORT = 3001;

// Placeholder endpoints - to be implemented with actual Playwright automation

// ARCA (AFIP) download
app.post('/arca/download', async (req, res) => {
    try {
        const { cuit, clave, cuitEmpresa } = req.body;
        if (!cuit || !clave) {
            return res.status(400).json({ error: 'CUIT y clave son requeridos' });
        }
        // TODO: Implement ARCA scraping with Playwright
        res.status(501).json({ error: 'ARCA scraping not yet implemented' });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// WhatsApp Web endpoints
let waSession = { linked: false, isLinking: false, info: null };

app.post('/whatsapp/link', async (req, res) => {
    waSession.isLinking = true;
    // TODO: Implement WhatsApp Web automation with Playwright
    res.json({ message: 'Linking started' });
});

app.get('/whatsapp/status', (req, res) => {
    res.json(waSession);
});

app.get('/whatsapp/qr', (req, res) => {
    // TODO: Return QR screenshot
    res.status(404).json({ error: 'QR not available' });
});

app.get('/whatsapp/check-linked', (req, res) => {
    res.json({ linked: waSession.linked });
});

app.post('/whatsapp/unlink', (req, res) => {
    waSession = { linked: false, isLinking: false, info: null };
    res.json({ message: 'Unlinked' });
});

app.post('/whatsapp/cancel-link', (req, res) => {
    waSession.isLinking = false;
    res.json({ message: 'Cancelled' });
});

app.post('/whatsapp/send-bulk', async (req, res) => {
    const { recipients, message } = req.body;
    // TODO: Implement bulk send with Playwright
    const results = (recipients || []).map(r => ({
        phone: r.phone,
        name: r.name,
        success: false,
        message: 'Not yet implemented'
    }));
    res.json(results);
});

app.listen(PORT, () => {
    console.log(`Playwright service running on port ${PORT}`);
});
