using FluxoDeCaixaAuth; // AddFluxoDeCaixaAuth extension
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFluxoDeCaixaAuth(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/login", ([FromQuery] string? returnUrl) =>
{
    var ret = string.IsNullOrWhiteSpace(returnUrl) ? "/home" : returnUrl;
    var html = @"<!doctype html><html><head><meta charset='utf-8'><title>Login</title><style>
body{font-family:Segoe UI,Arial;margin:40px;max-width:480px}
label{display:block;margin-top:12px;font-weight:600}
input[type=text],input[type=password]{width:100%;padding:8px;margin-top:4px;border:1px solid #bbb;border-radius:4px}
button{margin-top:16px;padding:10px 18px;background:#2563eb;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:15px}
.error{color:#b91c1c;margin-top:12px}
</style></head><body>
<h2>Auth Service</h2>
<form id='f'>
  <input type='hidden' name='returnUrl' value='" + System.Net.WebUtility.HtmlEncode(ret) + @"'>
  <label>Usuário<input name='username' value='admin' autofocus></label>
  <label>Senha<input name='password' type='password' value='password'></label>
  <button type='submit'>Entrar</button>
  <div id='msg' class='error' style='display:none'></div>
</form>
<script>
const f=document.getElementById('f');
f.addEventListener('submit',async e=>{e.preventDefault();
 const fd=new FormData(f); const body=JSON.stringify({username:fd.get('username'),password:fd.get('password')});
 const r=await fetch('/auth/token',{method:'POST',headers:{'Content-Type':'application/json','Accept':'application/json'},body});
 if(r.ok){ const ret=fd.get('returnUrl')||'/'; window.location=ret; } else { document.getElementById('msg').style.display='block'; document.getElementById('msg').textContent='Credenciais inválidas'; }
});
</script>
</body></html>";
    return Results.Content(html, "text/html; charset=utf-8");
}).AllowAnonymous();

// Secured home page: requires authentication + role consolidator
app.MapGet("/home", ([FromQuery] string? date) =>
{
    var today = DateTime.UtcNow.Date;
    var d = today.ToString("yyyy-MM-dd");
    var consolidacaoUrl = $"http://localhost:5260/?data={d}"; // página inicial do ConsolidacaoService (ajuste conforme UI)
    var lancamentosUrl = "http://localhost:5007/";
    var logoutUrl = "/auth/logout?returnUrl=/login"; // logout then go back to login
    var html = $@"<!doctype html><html><head><meta charset='utf-8'><title>Home</title><style>
body{{font-family:Segoe UI,Arial;margin:40px;max-width:640px}}
nav a{{margin-right:18px;text-decoration:none;color:#2563eb;font-weight:600}}
section{{margin-top:32px}}
code{{background:#f3f3f3;padding:2px 4px;border-radius:3px}}
</style></head><body>
<h1>Home</h1>
<nav>
 <a href='{lancamentosUrl}'>Lançamentos</a>
 <a href='{consolidacaoUrl}'>Consolidação (Hoje)</a>
 <a href='{logoutUrl}'>Logout</a>
</nav>
<section>
 <p>Bem-vindo! Utilize os links acima para navegar.</p>
 <p>Data corrente: {d}</p>
</section>
</body></html>";
    return Results.Content(html, "text/html; charset=utf-8");
}).RequireAuthorization("Consolidator");

// Root path -> redirect to login (preserva returnUrl se fornecido como query)
app.MapGet("/", ([FromQuery] string? returnUrl) =>
    Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/login" : $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}")
).AllowAnonymous();

app.Run();

