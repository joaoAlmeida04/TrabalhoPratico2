// ============================================================
//  Configuração da API
//  Ajuste BASE_URL para a URL onde o back-end está rodando.
// ============================================================
const API = {
  BASE_URL: 'http://localhost:5000/api',

  async request(path, options = {}) {
    const url = `${this.BASE_URL}${path}`;
    const opts = {
      headers: { 'Content-Type': 'application/json' },
      ...options
    };
    if (opts.body && typeof opts.body !== 'string') {
      opts.body = JSON.stringify(opts.body);
    }
    const resp = await fetch(url, opts);
    if (resp.status === 204) return null;

    let data = null;
    const texto = await resp.text();
    if (texto) {
      try { data = JSON.parse(texto); } catch { data = texto; }
    }
    if (!resp.ok) {
      const msg = (data && (data.mensagem || data.title)) || `Erro ${resp.status}`;
      throw new Error(msg);
    }
    return data;
  },

  get(path)        { return this.request(path); },
  post(path, body) { return this.request(path, { method: 'POST', body }); },
  put(path, body)  { return this.request(path, { method: 'PUT', body }); },
  del(path)        { return this.request(path, { method: 'DELETE' }); },

  // Monta query string ignorando valores vazios
  qs(params) {
    const p = new URLSearchParams();
    Object.entries(params).forEach(([k, v]) => {
      if (v !== '' && v !== null && v !== undefined) p.append(k, v);
    });
    const s = p.toString();
    return s ? `?${s}` : '';
  }
};

// ============================================================
//  Utilitários de interface
// ============================================================
function toast(msg, tipo = 'ok') {
  const box = document.getElementById('toast');
  const el = document.createElement('div');
  el.className = `toast-item ${tipo}`;
  el.textContent = msg;
  box.appendChild(el);
  setTimeout(() => {
    el.style.opacity = '0';
    el.style.transition = 'opacity .3s';
    setTimeout(() => el.remove(), 300);
  }, 3500);
}

function fmtMoeda(v) {
  return (v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function fmtData(d) {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('pt-BR');
}

function abrirModal(html) {
  const bg = document.getElementById('modal-bg');
  document.getElementById('modal-conteudo').innerHTML = html;
  bg.classList.add('open');
}

function fecharModal() {
  document.getElementById('modal-bg').classList.remove('open');
}

// Verifica se a API está online (indicador no cabeçalho)
async function checarStatusApi() {
  const el = document.querySelector('.api-status');
  try {
    await API.get('/fabricantes');
    el.classList.add('online');
    el.querySelector('.texto').textContent = 'API conectada';
  } catch {
    el.classList.remove('online');
    el.querySelector('.texto').textContent = 'API offline';
  }
}
