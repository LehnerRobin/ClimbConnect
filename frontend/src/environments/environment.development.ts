export const environment = {
  production: false,
  apiUrl: 'http://localhost:5004',
  // Pinnwand + Melden + Admin-Fälle im Browser simulieren, solange das Backend sie noch nicht hat.
  // Sobald die Endpoints aus PINNWAND-MELDEN-API.md im Backend existieren: auf false setzen.
  mockPinnwand: true
};
