const app = document.getElementById('app');
document.getElementById('nav-areas').onclick = (e)=>{ e.preventDefault(); showAreas(); };
document.getElementById('nav-profile').onclick = (e)=>{ e.preventDefault(); showProfile(); };
document.getElementById('nav-appointments').onclick = (e)=>{ e.preventDefault(); showAppointments(); };

function showAreas(){
  app.innerHTML = `<h2>Gebiete</h2>
    <div class="card">Mustergebiet – Linz Umgebung (Beispiel)</div>
    <div class="card">Mustergebiet – Mühlviertel (Beispiel)</div>`;
}
function showProfile(){
  app.innerHTML = `<h2>Profil</h2>
    <div class="card">Fortschritt: 12 Routen, Ø UIAA 6</div>`;
}
function showAppointments(){
  app.innerHTML = `<h2>Termine (Subscribe)</h2>
    <div class="card">
      <strong>Sa 14:00 – Pesenbachklamm</strong><br/>
      Treffpunkt: Parkplatz Süd<br/>
      <button>Mitklettern</button> <button class="secondary">Details</button>
    </div>`;
}
