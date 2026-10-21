namespace TournamentServices.Api;

public static class HomePage
{
    public const string Html = """
        <!doctype html>
        <html lang="es">
        <head>
          <meta charset="utf-8" />
          <title>Tournament Services</title>
          <style>
            html, body { background: #ffffff; color: #1a1a1a; }
            body { font-family: Segoe UI, sans-serif; margin: 2rem auto; max-width: 42rem; }
            a { color: #0b57d0; }
            code { background: #f3f3f3; padding: 0.1rem 0.3rem; }
            li { margin: 0.25rem 0; }
            .empty { color: #555; }
          </style>
        </head>
        <body>
          <h1>Tournament Services</h1>
          <p>La API está en marcha. Estado: <strong id="health">comprobando…</strong></p>
          <p>Para probar los endpoints abre <a href="/swagger">/swagger</a>.</p>
          <h2>Equipos</h2>
          <ul id="teams"><li class="empty">Cargando…</li></ul>
          <h2>Torneos</h2>
          <ul id="tournaments"><li class="empty">Cargando…</li></ul>
          <script>
            const list = (id, items, label) => {
              const node = document.getElementById(id);
              node.replaceChildren();
              if (!items.length) {
                const li = document.createElement("li");
                li.className = "empty";
                li.textContent = "Todavía no hay " + label + ". Créalos desde /swagger.";
                node.append(li);
                return;
              }
              for (const item of items) {
                const li = document.createElement("li");
                li.textContent = item.name + " (" + item.id + ")";
                node.append(li);
              }
            };
            fetch("/health").then(r => r.text()).then(t => {
              document.getElementById("health").textContent = t;
            });
            fetch("/teams").then(r => r.json()).then(items => list("teams", items, "equipos"));
            fetch("/tournaments").then(r => r.json()).then(items => list("tournaments", items, "torneos"));
          </script>
        </body>
        </html>
        """;
}
