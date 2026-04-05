const express = require('express');
const { chromium } = require('playwright');
const app = express();
app.use(express.json({ limit: '50mb' }));

const PORT = 3001;

// ===== ARCA (AFIP) =====
app.post('/arca/download', async (req, res) => {
    try {
        const { cuit, clave, cuitEmpresa } = req.body;
        if (!cuit || !clave) {
            return res.status(400).json({ error: 'CUIT y clave son requeridos' });
        }
        res.status(501).json({ error: 'ARCA scraping not yet implemented' });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// ===== WhatsApp Web =====
let waBrowser = null;
let waPage = null;
let waSession = { linked: false, isLinking: false, info: null };
let lastQrScreenshot = null;

async function cleanupWhatsApp() {
    try {
        if (waBrowser) {
            await waBrowser.close().catch(() => {});
        }
    } catch {}
    waBrowser = null;
    waPage = null;
    lastQrScreenshot = null;
}

app.post('/whatsapp/link', async (req, res) => {
    try {
        // Cleanup previous session if any
        await cleanupWhatsApp();

        waSession.isLinking = true;
        waSession.linked = false;
        waSession.info = null;

        // Launch browser
        waBrowser = await chromium.launch({
            headless: true,
            args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage']
        });

        const context = await waBrowser.newContext({
            userAgent: 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
            viewport: { width: 1280, height: 900 }
        });

        waPage = await context.newPage();
        await waPage.goto('https://web.whatsapp.com', { waitUntil: 'domcontentloaded', timeout: 60000 });

        // Wait a bit for QR to render
        await waPage.waitForTimeout(3000);

        // Take initial QR screenshot
        await captureQr();

        // Start background polling for linked status
        pollLinkedStatus();

        res.json({ message: 'Linking started' });
    } catch (err) {
        console.error('Error starting WhatsApp link:', err.message);
        waSession.isLinking = false;
        await cleanupWhatsApp();
        res.status(500).json({ error: err.message });
    }
});

async function captureQr() {
    if (!waPage) return;
    try {
        // Try to find the QR canvas element
        const qrCanvas = await waPage.$('canvas');
        if (qrCanvas) {
            lastQrScreenshot = await qrCanvas.screenshot({ type: 'png' });
            return;
        }

        // Fallback: screenshot the QR area or full page
        const qrContainer = await waPage.$('[data-ref]');
        if (qrContainer) {
            lastQrScreenshot = await qrContainer.screenshot({ type: 'png' });
            return;
        }

        // Last fallback: full page screenshot
        lastQrScreenshot = await waPage.screenshot({ type: 'png' });
    } catch (err) {
        console.error('Error capturing QR:', err.message);
    }
}

function pollLinkedStatus() {
    const interval = setInterval(async () => {
        if (!waPage || !waSession.isLinking) {
            clearInterval(interval);
            return;
        }

        try {
            // Refresh QR screenshot
            await captureQr();

            // Check if we're logged in (main chat screen appears)
            const chatList = await waPage.$('[data-testid="chat-list"]');
            const side = await waPage.$('#side');
            const pane = await waPage.$('#pane-side');

            if (chatList || side || pane) {
                waSession.linked = true;
                waSession.isLinking = false;
                waSession.info = 'Conectado';
                clearInterval(interval);
                console.log('WhatsApp linked successfully');
            }
        } catch (err) {
            console.error('Polling error:', err.message);
        }
    }, 2000);

    // Timeout after 2 minutes
    setTimeout(() => {
        clearInterval(interval);
        if (!waSession.linked && waSession.isLinking) {
            waSession.isLinking = false;
            console.log('WhatsApp linking timed out');
            cleanupWhatsApp();
        }
    }, 120000);
}

app.get('/whatsapp/status', (req, res) => {
    res.json(waSession);
});

app.get('/whatsapp/qr', async (req, res) => {
    // Refresh screenshot on each request
    if (waPage && waSession.isLinking) {
        await captureQr();
    }

    if (!lastQrScreenshot) {
        return res.status(404).json({ error: 'QR not available' });
    }

    res.set('Content-Type', 'image/png');
    res.set('Cache-Control', 'no-cache, no-store');
    res.send(lastQrScreenshot);
});

app.get('/whatsapp/check-linked', (req, res) => {
    res.json({ linked: waSession.linked });
});

app.post('/whatsapp/unlink', async (req, res) => {
    await cleanupWhatsApp();
    waSession = { linked: false, isLinking: false, info: null };
    res.json({ message: 'Unlinked' });
});

app.post('/whatsapp/cancel-link', async (req, res) => {
    waSession.isLinking = false;
    await cleanupWhatsApp();
    res.json({ message: 'Cancelled' });
});

app.post('/whatsapp/send-bulk', async (req, res) => {
    const { recipients, message } = req.body;

    if (!waSession.linked || !waPage) {
        return res.status(400).json({ error: 'WhatsApp no esta vinculado' });
    }

    const results = [];
    for (const r of (recipients || [])) {
        try {
            // Navigate to chat with phone number
            await waPage.goto(`https://web.whatsapp.com/send?phone=${r.phone}&text=${encodeURIComponent(message)}`, {
                waitUntil: 'domcontentloaded',
                timeout: 30000
            });

            // Wait for message input to appear
            await waPage.waitForSelector('[data-testid="conversation-compose-box-input"], [contenteditable="true"][data-tab="10"]', { timeout: 15000 });
            await waPage.waitForTimeout(1000);

            // Press Enter to send
            await waPage.keyboard.press('Enter');
            await waPage.waitForTimeout(2000);

            results.push({ phone: r.phone, name: r.name, success: true, message: 'Enviado' });
        } catch (err) {
            results.push({ phone: r.phone, name: r.name, success: false, message: err.message });
        }
    }

    res.json(results);
});

app.listen(PORT, () => {
    console.log(`Playwright service running on port ${PORT}`);
});
