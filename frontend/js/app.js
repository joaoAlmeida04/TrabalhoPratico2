// ============================================================
//  Estado e navegação
// ============================================================
let cacheFabricantes = [];
let cacheCategorias = [];
let cacheClientes = [];
let cacheVeiculos = [];

document.addEventListener('DOMContentLoaded', () => {
  // Navegação entre abas
  document.querySelectorAll('nav button').forEach(btn => {
    btn.addEventListener('click', () => navegar(btn.dataset.view));
  });
  // Fechar modal clicando fora
  document.getElementById('modal-bg').addEventListener('click', e => {
    if (e.target.id === 'modal-bg') fecharModal();
  });
  checarStatusApi();
  navegar('dashboard');
});

function navegar(view) {
  document.querySelectorAll('nav button').forEach(b =>
    b.classList.toggle('active', b.dataset.view === view));
  document.querySelectorAll('.view').forEach(v =>
    v.classList.toggle('active', v.id === `view-${view}`));

  switch (view) {
    case 'dashboard':   carregarDashboard(); break;
    case 'veiculos':    carregarVeiculos(); break;
    case 'clientes':    carregarClientes(); break;
    case 'alugueis':    carregarAlugueis(); break;
    case 'fabricantes': carregarFabricantes(); break;
    case 'pagamentos':  carregarPagamentos(); break;
  }
}

// Pré-carrega listas usadas em selects
async function carregarReferencias() {
  try {
    [cacheFabricantes, cacheCategorias, cacheClientes, cacheVeiculos] = await Promise.all([
      API.get('/fabricantes'),
      API.get('/categorias'),
      API.get('/clientes'),
      API.get('/veiculos')
    ]);
  } catch (e) { /* silencioso; usado sob demanda */ }
}

function opcoes(lista, valor, texto, selecionado) {
  return lista.map(i =>
    `<option value="${i[valor]}" ${i[valor] == selecionado ? 'selected' : ''}>${i[texto]}</option>`
  ).join('');
}

// ============================================================
//  DASHBOARD  (consome filtros com JOIN: faturamento, resumo)
// ============================================================
async function carregarDashboard() {
  const cont = document.getElementById('view-dashboard');
  try {
    const [veiculos, clientes, alugueis, faturamento, contagem] = await Promise.all([
      API.get('/veiculos'),
      API.get('/clientes'),
      API.get('/alugueis'),
      API.get('/alugueis/faturamento-por-fabricante'),
      API.get('/veiculos/contagem-por-fabricante')
    ]);

    const disponiveis = veiculos.filter(v => v.disponivel).length;
    const emAndamento = alugueis.filter(a => !a.devolvido).length;
    const totalFat = faturamento.reduce((s, f) => s + f.faturamento, 0);

    cont.querySelector('.conteudo').innerHTML = `
      <div class="cards">
        <div class="card"><div class="valor">${veiculos.length}</div><div class="rotulo">Veículos cadastrados</div></div>
        <div class="card"><div class="valor">${disponiveis}</div><div class="rotulo">Disponíveis</div></div>
        <div class="card"><div class="valor">${clientes.length}</div><div class="rotulo">Clientes</div></div>
        <div class="card"><div class="valor">${emAndamento}</div><div class="rotulo">Aluguéis ativos</div></div>
        <div class="card"><div class="valor">${fmtMoeda(totalFat)}</div><div class="rotulo">Faturamento total</div></div>
      </div>

      <div class="painel">
        <h3>Faturamento por fabricante <small style="color:var(--text-dim)">(JOIN Aluguel → Veículo → Fabricante)</small></h3>
        <div class="tabela-wrap">
          <table>
            <thead><tr><th>Fabricante</th><th>Qtd. aluguéis</th><th>Faturamento</th></tr></thead>
            <tbody>
              ${faturamento.length ? faturamento.map(f => `
                <tr><td>${f.fabricante}</td><td>${f.qtdAlugueis}</td><td>${fmtMoeda(f.faturamento)}</td></tr>
              `).join('') : '<tr><td colspan="3" class="vazio">Sem dados</td></tr>'}
            </tbody>
          </table>
        </div>
      </div>

      <div class="painel">
        <h3>Veículos por fabricante <small style="color:var(--text-dim)">(JOIN Veículo → Fabricante)</small></h3>
        <div class="tabela-wrap">
          <table>
            <thead><tr><th>Fabricante</th><th>Quantidade</th></tr></thead>
            <tbody>
              ${contagem.map(c => `<tr><td>${c.fabricante}</td><td>${c.quantidade}</td></tr>`).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  } catch (e) {
    cont.querySelector('.conteudo').innerHTML =
      `<div class="vazio">Não foi possível carregar o dashboard.<br>Verifique se a API está rodando em <code>${API.BASE_URL}</code>.<br><br>${e.message}</div>`;
  }
}

// ============================================================
//  VEÍCULOS  (filtros: modelo, fabricante, categoria, disponível)
// ============================================================
async function carregarVeiculos() {
  await carregarReferencias();
  document.getElementById('flt-veic-fabricante').innerHTML =
    `<option value="">Todos</option>` + opcoes(cacheFabricantes, 'id', 'nome');
  document.getElementById('flt-veic-categoria').innerHTML =
    `<option value="">Todas</option>` + opcoes(cacheCategorias, 'id', 'nome');
  filtrarVeiculos();
}

async function filtrarVeiculos() {
  const params = {
    modelo: document.getElementById('flt-veic-modelo').value,
    fabricanteId: document.getElementById('flt-veic-fabricante').value,
    categoriaId: document.getElementById('flt-veic-categoria').value,
    disponivel: document.getElementById('flt-veic-disp').value
  };
  const tbody = document.getElementById('tbody-veiculos');
  try {
    const lista = await API.get('/veiculos' + API.qs(params));
    tbody.innerHTML = lista.length ? lista.map(v => `
      <tr>
        <td>${v.id}</td>
        <td>${v.modelo}</td>
        <td>${v.fabricanteNome ?? '—'}</td>
        <td>${v.categoriaNome ?? '—'}</td>
        <td>${v.anoFabricacao}</td>
        <td>${v.quilometragem.toLocaleString('pt-BR')} km</td>
        <td>${fmtMoeda(v.valorDiariaBase)}</td>
        <td>${v.disponivel ? '<span class="badge ok">Disponível</span>' : '<span class="badge no">Alugado</span>'}</td>
        <td class="acoes">
          <button class="btn small secondary" onclick="formVeiculo(${v.id})">Editar</button>
          <button class="btn small danger" onclick="excluirVeiculo(${v.id})">Excluir</button>
        </td>
      </tr>`).join('') : '<tr><td colspan="9" class="vazio">Nenhum veículo encontrado</td></tr>';
  } catch (e) { toast(e.message, 'err'); }
}

async function formVeiculo(id) {
  await carregarReferencias();
  let v = { modelo: '', anoFabricacao: 2024, quilometragem: 0, placa: '', cor: '', valorDiariaBase: 0, disponivel: true, fabricanteId: '', categoriaVeiculoId: '' };
  if (id) v = await API.get(`/veiculos/${id}`);

  abrirModal(`
    <div class="modal-head"><h3>${id ? 'Editar' : 'Novo'} veículo</h3><button class="close" onclick="fecharModal()">&times;</button></div>
    <div class="modal-body">
      <div class="form-grid">
        <div class="campo full"><label>Modelo *</label><input id="v-modelo" value="${v.modelo}"></div>
        <div class="campo"><label>Fabricante *</label><select id="v-fab"><option value="">Selecione</option>${opcoes(cacheFabricantes,'id','nome',v.fabricanteId)}</select></div>
        <div class="campo"><label>Categoria *</label><select id="v-cat"><option value="">Selecione</option>${opcoes(cacheCategorias,'id','nome',v.categoriaVeiculoId)}</select></div>
        <div class="campo"><label>Ano *</label><input id="v-ano" type="number" value="${v.anoFabricacao}"></div>
        <div class="campo"><label>Quilometragem *</label><input id="v-km" type="number" value="${v.quilometragem}"></div>
        <div class="campo"><label>Placa</label><input id="v-placa" value="${v.placa ?? ''}" maxlength="8"></div>
        <div class="campo"><label>Cor</label><input id="v-cor" value="${v.cor ?? ''}"></div>
        <div class="campo"><label>Diária base (R$) *</label><input id="v-diaria" type="number" step="0.01" value="${v.valorDiariaBase}"></div>
        <div class="campo"><label>Disponível</label><select id="v-disp"><option value="true" ${v.disponivel?'selected':''}>Sim</option><option value="false" ${!v.disponivel?'selected':''}>Não</option></select></div>
      </div>
    </div>
    <div class="modal-foot">
      <button class="btn secondary" onclick="fecharModal()">Cancelar</button>
      <button class="btn" onclick="salvarVeiculo(${id || 'null'})">Salvar</button>
    </div>
  `);
}

async function salvarVeiculo(id) {
  const body = {
    modelo: document.getElementById('v-modelo').value.trim(),
    fabricanteId: parseInt(document.getElementById('v-fab').value),
    categoriaVeiculoId: parseInt(document.getElementById('v-cat').value),
    anoFabricacao: parseInt(document.getElementById('v-ano').value),
    quilometragem: parseInt(document.getElementById('v-km').value),
    placa: document.getElementById('v-placa').value.trim() || null,
    cor: document.getElementById('v-cor').value.trim() || null,
    valorDiariaBase: parseFloat(document.getElementById('v-diaria').value),
    disponivel: document.getElementById('v-disp').value === 'true'
  };
  if (!body.modelo || !body.fabricanteId || !body.categoriaVeiculoId) {
    return toast('Preencha os campos obrigatórios.', 'err');
  }
  try {
    if (id) { await API.put(`/veiculos/${id}`, body); toast('Veículo atualizado!'); }
    else    { await API.post('/veiculos', body); toast('Veículo cadastrado!'); }
    fecharModal();
    filtrarVeiculos();
  } catch (e) { toast(e.message, 'err'); }
}

async function excluirVeiculo(id) {
  if (!confirm('Excluir este veículo?')) return;
  try { await API.del(`/veiculos/${id}`); toast('Veículo excluído.'); filtrarVeiculos(); }
  catch (e) { toast(e.message, 'err'); }
}

// ============================================================
//  CLIENTES  (filtros: nome, cidade) + recarga/saldo
// ============================================================
async function carregarClientes() { filtrarClientes(); }

async function filtrarClientes() {
  const params = {
    nome: document.getElementById('flt-cli-nome').value,
    cidade: document.getElementById('flt-cli-cidade').value
  };
  const tbody = document.getElementById('tbody-clientes');
  try {
    const lista = await API.get('/clientes' + API.qs(params));
    tbody.innerHTML = lista.length ? lista.map(c => `
      <tr>
        <td>${c.id}</td>
        <td>${c.nome}</td>
        <td>${c.cpf}</td>
        <td>${c.email}</td>
        <td>${c.cidade ?? '—'}</td>
        <td><strong>${fmtMoeda(c.saldo)}</strong></td>
        <td class="acoes">
          <button class="btn small success" onclick="formRecarga(${c.id}, '${c.nome.replace(/'/g,"")}')">Recarregar</button>
          <button class="btn small secondary" onclick="formCliente(${c.id})">Editar</button>
          <button class="btn small danger" onclick="excluirCliente(${c.id})">Excluir</button>
        </td>
      </tr>`).join('') : '<tr><td colspan="7" class="vazio">Nenhum cliente encontrado</td></tr>';
  } catch (e) { toast(e.message, 'err'); }
}

async function formCliente(id) {
  let c = { nome: '', cpf: '', email: '', telefone: '', cidade: '' };
  if (id) c = await API.get(`/clientes/${id}`);
  abrirModal(`
    <div class="modal-head"><h3>${id ? 'Editar' : 'Novo'} cliente</h3><button class="close" onclick="fecharModal()">&times;</button></div>
    <div class="modal-body">
      <div class="form-grid">
        <div class="campo full"><label>Nome *</label><input id="c-nome" value="${c.nome}"></div>
        <div class="campo"><label>CPF *</label><input id="c-cpf" value="${c.cpf}" placeholder="000.000.000-00"></div>
        <div class="campo"><label>E-mail *</label><input id="c-email" type="email" value="${c.email}"></div>
        <div class="campo"><label>Telefone</label><input id="c-tel" value="${c.telefone ?? ''}"></div>
        <div class="campo"><label>Cidade</label><input id="c-cidade" value="${c.cidade ?? ''}"></div>
      </div>
    </div>
    <div class="modal-foot">
      <button class="btn secondary" onclick="fecharModal()">Cancelar</button>
      <button class="btn" onclick="salvarCliente(${id || 'null'})">Salvar</button>
    </div>
  `);
}

async function salvarCliente(id) {
  const body = {
    nome: document.getElementById('c-nome').value.trim(),
    cpf: document.getElementById('c-cpf').value.trim(),
    email: document.getElementById('c-email').value.trim(),
    telefone: document.getElementById('c-tel').value.trim() || null,
    cidade: document.getElementById('c-cidade').value.trim() || null
  };
  if (!body.nome || !body.cpf || !body.email) return toast('Preencha nome, CPF e e-mail.', 'err');
  try {
    if (id) { await API.put(`/clientes/${id}`, body); toast('Cliente atualizado!'); }
    else    { await API.post('/clientes', body); toast('Cliente cadastrado!'); }
    fecharModal(); filtrarClientes();
  } catch (e) { toast(e.message, 'err'); }
}

function formRecarga(id, nome) {
  abrirModal(`
    <div class="modal-head"><h3>Recarregar saldo</h3><button class="close" onclick="fecharModal()">&times;</button></div>
    <div class="modal-body">
      <p style="margin-bottom:14px;color:var(--text-dim)">Cliente: <strong style="color:var(--text)">${nome}</strong></p>
      <div class="campo"><label>Valor da recarga (R$)</label><input id="r-valor" type="number" step="0.01" min="0.01" placeholder="0,00"></div>
    </div>
    <div class="modal-foot">
      <button class="btn secondary" onclick="fecharModal()">Cancelar</button>
      <button class="btn success" onclick="salvarRecarga(${id})">Recarregar</button>
    </div>
  `);
}

async function salvarRecarga(id) {
  const valor = parseFloat(document.getElementById('r-valor').value);
  if (!valor || valor <= 0) return toast('Informe um valor válido.', 'err');
  try {
    const r = await API.post(`/clientes/${id}/recarga`, { valor });
    toast(`Recarga feita! Novo saldo: ${fmtMoeda(r.novoSaldo)}`);
    fecharModal(); filtrarClientes();
  } catch (e) { toast(e.message, 'err'); }
}

async function excluirCliente(id) {
  if (!confirm('Excluir este cliente?')) return;
  try { await API.del(`/clientes/${id}`); toast('Cliente excluído.'); filtrarClientes(); }
  catch (e) { toast(e.message, 'err'); }
}

// ============================================================
//  ALUGUÉIS  (filtros: cliente, status) + devolução
// ============================================================
async function carregarAlugueis() {
  await carregarReferencias();
  document.getElementById('flt-alu-cliente').innerHTML =
    `<option value="">Todos</option>` + opcoes(cacheClientes, 'id', 'nome');
  filtrarAlugueis();
}

async function filtrarAlugueis() {
  const params = {
    clienteId: document.getElementById('flt-alu-cliente').value,
    devolvido: document.getElementById('flt-alu-status').value
  };
  const tbody = document.getElementById('tbody-alugueis');
  try {
    const lista = await API.get('/alugueis' + API.qs(params));
    tbody.innerHTML = lista.length ? lista.map(a => `
      <tr>
        <td>${a.id}</td>
        <td>${a.clienteNome ?? '—'}</td>
        <td>${a.veiculoModelo ?? '—'}</td>
        <td>${fmtData(a.dataRetirada)}</td>
        <td>${fmtData(a.dataDevolucaoPrevista)}</td>
        <td>${fmtMoeda(a.valorTotal)}</td>
        <td>${a.devolvido ? '<span class="badge ok">Devolvido</span>' : '<span class="badge warn">Em andamento</span>'}</td>
        <td class="acoes">
          ${!a.devolvido ? `<button class="btn small success" onclick="formDevolucao(${a.id}, ${a.quilometragemInicial})">Devolver</button>` : ''}
          <button class="btn small danger" onclick="excluirAluguel(${a.id})">Excluir</button>
        </td>
      </tr>`).join('') : '<tr><td colspan="8" class="vazio">Nenhum aluguel encontrado</td></tr>';
  } catch (e) { toast(e.message, 'err'); }
}

async function formAluguel() {
  await carregarReferencias();
  const disponiveis = cacheVeiculos.filter(v => v.disponivel);
  abrirModal(`
    <div class="modal-head"><h3>Novo aluguel</h3><button class="close" onclick="fecharModal()">&times;</button></div>
    <div class="modal-body">
      <div class="form-grid">
        <div class="campo full"><label>Cliente *</label><select id="a-cliente"><option value="">Selecione</option>${opcoes(cacheClientes,'id','nome')}</select></div>
        <div class="campo full"><label>Veículo (disponíveis) *</label>
          <select id="a-veiculo"><option value="">Selecione</option>
          ${disponiveis.map(v => `<option value="${v.id}">${v.modelo} — ${v.fabricanteNome} (${fmtMoeda(v.valorDiariaBase)}/dia)</option>`).join('')}
          </select></div>
        <div class="campo"><label>Data retirada *</label><input id="a-retirada" type="date"></div>
        <div class="campo"><label>Devolução prevista *</label><input id="a-devolucao" type="date"></div>
        <div class="campo full"><label>Km inicial *</label><input id="a-km" type="number" value="0"></div>
      </div>
    </div>
    <div class="modal-foot">
      <button class="btn secondary" onclick="fecharModal()">Cancelar</button>
      <button class="btn" onclick="salvarAluguel()">Registrar aluguel</button>
    </div>
  `);
}

async function salvarAluguel() {
  const body = {
    clienteId: parseInt(document.getElementById('a-cliente').value),
    veiculoId: parseInt(document.getElementById('a-veiculo').value),
    dataRetirada: document.getElementById('a-retirada').value,
    dataDevolucaoPrevista: document.getElementById('a-devolucao').value,
    quilometragemInicial: parseInt(document.getElementById('a-km').value) || 0
  };
  if (!body.clienteId || !body.veiculoId || !body.dataRetirada || !body.dataDevolucaoPrevista)
    return toast('Preencha cliente, veículo e datas.', 'err');
  try {
    await API.post('/alugueis', body);
    toast('Aluguel registrado!');
    fecharModal(); filtrarAlugueis();
  } catch (e) { toast(e.message, 'err'); }
}

function formDevolucao(id, kmInicial) {
  abrirModal(`
    <div class="modal-head"><h3>Registrar devolução</h3><button class="close" onclick="fecharModal()">&times;</button></div>
    <div class="modal-body">
      <div class="form-grid">
        <div class="campo"><label>Data da devolução *</label><input id="d-data" type="date" value="${new Date().toISOString().slice(0,10)}"></div>
        <div class="campo"><label>Km final * (≥ ${kmInicial})</label><input id="d-km" type="number" value="${kmInicial}"></div>
      </div>
    </div>
    <div class="modal-foot">
      <button class="btn secondary" onclick="fecharModal()">Cancelar</button>
      <button class="btn success" onclick="salvarDevolucao(${id})">Confirmar devolução</button>
    </div>
  `);
}

async function salvarDevolucao(id) {
  const body = {
    dataDevolucaoReal: document.getElementById('d-data').value,
    quilometragemFinal: parseInt(document.getElementById('d-km').value)
  };
  if (!body.dataDevolucaoReal) return toast('Informe a data da devolução.', 'err');
  try {
    await API.put(`/alugueis/${id}/devolucao`, body);
    toast('Devolução registrada!');
    fecharModal(); filtrarAlugueis();
  } catch (e) { toast(e.message, 'err'); }
}

async function excluirAluguel(id) {
  if (!confirm('Excluir este aluguel?')) return;
  try { await API.del(`/alugueis/${id}`); toast('Aluguel excluído.'); filtrarAlugueis(); }
  catch (e) { toast(e.message, 'err'); }
}

// ============================================================
//  FABRICANTES (CRUD simples, filtro por nome)
// ============================================================
async function carregarFabricantes() { filtrarFabricantes(); }

async function filtrarFabricantes() {
  const nome = document.getElementById('flt-fab-nome').value;
  const tbody = document.getElementById('tbody-fabricantes');
  try {
    const lista = await API.get('/fabricantes' + API.qs({ nome }));
    tbody.innerHTML = lista.length ? lista.map(f => `
      <tr>
        <td>${f.id}</td>
        <td>${f.nome}</td>
        <td>${f.paisOrigem ?? '—'}</td>
        <td class="acoes">
          <button class="btn small secondary" onclick="formFabricante(${f.id})">Editar</button>
          <button class="btn small danger" onclick="excluirFabricante(${f.id})">Excluir</button>
        </td>
      </tr>`).join('') : '<tr><td colspan="4" class="vazio">Nenhum fabricante</td></tr>';
  } catch (e) { toast(e.message, 'err'); }
}

async function formFabricante(id) {
  let f = { nome: '', paisOrigem: '' };
  if (id) f = await API.get(`/fabricantes/${id}`);
  abrirModal(`
    <div class="modal-head"><h3>${id ? 'Editar' : 'Novo'} fabricante</h3><button class="close" onclick="fecharModal()">&times;</button></div>
    <div class="modal-body">
      <div class="form-grid">
        <div class="campo full"><label>Nome *</label><input id="f-nome" value="${f.nome}"></div>
        <div class="campo full"><label>País de origem</label><input id="f-pais" value="${f.paisOrigem ?? ''}"></div>
      </div>
    </div>
    <div class="modal-foot">
      <button class="btn secondary" onclick="fecharModal()">Cancelar</button>
      <button class="btn" onclick="salvarFabricante(${id || 'null'})">Salvar</button>
    </div>
  `);
}

async function salvarFabricante(id) {
  const body = {
    nome: document.getElementById('f-nome').value.trim(),
    paisOrigem: document.getElementById('f-pais').value.trim() || null
  };
  if (!body.nome) return toast('Informe o nome.', 'err');
  try {
    if (id) { await API.put(`/fabricantes/${id}`, body); toast('Fabricante atualizado!'); }
    else    { await API.post('/fabricantes', body); toast('Fabricante cadastrado!'); }
    fecharModal(); filtrarFabricantes();
  } catch (e) { toast(e.message, 'err'); }
}

async function excluirFabricante(id) {
  if (!confirm('Excluir este fabricante?')) return;
  try { await API.del(`/fabricantes/${id}`); toast('Fabricante excluído.'); filtrarFabricantes(); }
  catch (e) { toast(e.message, 'err'); }
}

// ============================================================
//  PAGAMENTOS  (registro com débito de saldo + consulta)
// ============================================================
async function carregarPagamentos() { filtrarPagamentos(); }

async function filtrarPagamentos() {
  const aluguelId = document.getElementById('flt-pag-aluguel').value;
  const tbody = document.getElementById('tbody-pagamentos');
  const formas = ['Saldo','Cartão de crédito','Cartão de débito','Pix','Dinheiro'];
  try {
    const lista = await API.get('/pagamentos' + API.qs({ aluguelId }));
    tbody.innerHTML = lista.length ? lista.map(p => `
      <tr>
        <td>${p.id}</td>
        <td>#${p.aluguelId}</td>
        <td>${fmtMoeda(p.valor)}</td>
        <td>${formas[p.forma] ?? p.forma}</td>
        <td>${fmtData(p.dataPagamento)}</td>
        <td>${p.confirmado ? '<span class="badge ok">Confirmado</span>' : '<span class="badge no">Pendente</span>'}</td>
        <td class="acoes"><button class="btn small danger" onclick="excluirPagamento(${p.id})">Estornar</button></td>
      </tr>`).join('') : '<tr><td colspan="7" class="vazio">Nenhum pagamento</td></tr>';
  } catch (e) { toast(e.message, 'err'); }
}

async function formPagamento() {
  const alugueis = await API.get('/alugueis');
  abrirModal(`
    <div class="modal-head"><h3>Registrar pagamento</h3><button class="close" onclick="fecharModal()">&times;</button></div>
    <div class="modal-body">
      <div class="form-grid">
        <div class="campo full"><label>Aluguel *</label>
          <select id="p-aluguel"><option value="">Selecione</option>
          ${alugueis.map(a => `<option value="${a.id}">#${a.id} — ${a.clienteNome} / ${a.veiculoModelo} (${fmtMoeda(a.valorTotal)})</option>`).join('')}
          </select></div>
        <div class="campo"><label>Valor (R$) *</label><input id="p-valor" type="number" step="0.01"></div>
        <div class="campo"><label>Forma *</label>
          <select id="p-forma">
            <option value="0">Saldo (carteira)</option>
            <option value="1">Cartão de crédito</option>
            <option value="2">Cartão de débito</option>
            <option value="3">Pix</option>
            <option value="4">Dinheiro</option>
          </select></div>
      </div>
      <p style="margin-top:12px;font-size:.8rem;color:var(--text-dim)">Pagamentos via "Saldo" debitam a carteira do cliente.</p>
    </div>
    <div class="modal-foot">
      <button class="btn secondary" onclick="fecharModal()">Cancelar</button>
      <button class="btn success" onclick="salvarPagamento()">Pagar</button>
    </div>
  `);
}

async function salvarPagamento() {
  const body = {
    aluguelId: parseInt(document.getElementById('p-aluguel').value),
    valor: parseFloat(document.getElementById('p-valor').value),
    forma: parseInt(document.getElementById('p-forma').value)
  };
  if (!body.aluguelId || !body.valor) return toast('Selecione o aluguel e o valor.', 'err');
  try {
    await API.post('/pagamentos', body);
    toast('Pagamento registrado!');
    fecharModal(); filtrarPagamentos();
  } catch (e) { toast(e.message, 'err'); }
}

async function excluirPagamento(id) {
  if (!confirm('Estornar este pagamento?')) return;
  try { await API.del(`/pagamentos/${id}`); toast('Pagamento estornado.'); filtrarPagamentos(); }
  catch (e) { toast(e.message, 'err'); }
}
