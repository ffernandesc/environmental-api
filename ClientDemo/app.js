const STORAGE_KEYS = {
  baseUrl: "environmentAlerts.apiBaseUrl",
  apiKey: "environmentAlerts.apiKey",
};

const elements = {
  apiBaseUrl: document.getElementById("apiBaseUrl"),
  apiKey: document.getElementById("apiKey"),
  saveConnectionButton: document.getElementById("saveConnectionButton"),
  healthCheckButton: document.getElementById("healthCheckButton"),
  connectionStatus: document.getElementById("connectionStatus"),

  locationForm: document.getElementById("locationForm"),
  locationStatus: document.getElementById("locationStatus"),
  locationsTableBody: document.getElementById("locationsTableBody"),

  readingForm: document.getElementById("readingForm"),
  readingLocation: document.getElementById("readingLocation"),
  readingStatus: document.getElementById("readingStatus"),
  readingsTableBody: document.getElementById("readingsTableBody"),
  importCurrentButton: document.getElementById("importCurrentButton"),

  refreshAlertsButton: document.getElementById("refreshAlertsButton"),
  alertStatus: document.getElementById("alertStatus"),
  alertsTableBody: document.getElementById("alertsTableBody"),
};

function getBaseUrl() {
  return elements.apiBaseUrl.value.trim().replace(/\/+$/, "");
}

function getApiKey() {
  return elements.apiKey.value.trim();
}

function showStatus(element, message, isError) {
  element.textContent = message;
  element.className = "status " + (isError ? "error" : "ok");
}

async function apiFetch(path, { method = "GET", body, requireAuth = false } = {}) {
  const baseUrl = getBaseUrl();

  if (!baseUrl) {
    throw new Error("Configura primeiro a URL da API.");
  }

  const headers = {};

  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  if (requireAuth) {
    const apiKey = getApiKey();

    if (!apiKey) {
      throw new Error("Configura a API Key para operações protegidas.");
    }

    headers["X-API-Key"] = apiKey;
  }

  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`${response.status} ${response.statusText}${text ? ` - ${text}` : ""}`);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

function loadConnectionSettings() {
  elements.apiBaseUrl.value = localStorage.getItem(STORAGE_KEYS.baseUrl) ?? "";
  elements.apiKey.value = localStorage.getItem(STORAGE_KEYS.apiKey) ?? "";
}

function saveConnectionSettings() {
  localStorage.setItem(STORAGE_KEYS.baseUrl, getBaseUrl());
  localStorage.setItem(STORAGE_KEYS.apiKey, getApiKey());
  showStatus(elements.connectionStatus, "Definições guardadas.", false);
}

async function checkHealth() {
  try {
    const baseUrl = getBaseUrl();

    if (!baseUrl) {
      throw new Error("Configura primeiro a URL da API.");
    }

    const response = await fetch(`${baseUrl}/`);
    const text = await response.text();

    if (!response.ok) {
      throw new Error(`${response.status} ${response.statusText}`);
    }

    showStatus(elements.connectionStatus, `Ligado: ${text}`, false);
  } catch (error) {
    showStatus(elements.connectionStatus, `Falha na ligação: ${error.message}`, true);
  }
}

function renderLocations(locations) {
  elements.locationsTableBody.innerHTML = "";
  elements.readingLocation.innerHTML = "";

  for (const location of locations) {
    const row = document.createElement("tr");
    row.innerHTML = `
      <td>${location.id}</td>
      <td>${location.city}</td>
      <td>${location.district}</td>
      <td>${location.latitude}</td>
      <td>${location.longitude}</td>
      <td class="inline-actions"></td>
    `;

    const deleteButton = document.createElement("button");
    deleteButton.textContent = "Eliminar";
    deleteButton.className = "danger";
    deleteButton.addEventListener("click", () => deleteLocation(location.id));
    row.querySelector(".inline-actions").appendChild(deleteButton);

    elements.locationsTableBody.appendChild(row);

    const option = document.createElement("option");
    option.value = location.id;
    option.textContent = `${location.city} (#${location.id})`;
    elements.readingLocation.appendChild(option);
  }
}

async function loadLocations() {
  try {
    const locations = await apiFetch("/api/locations");
    renderLocations(locations);
  } catch (error) {
    showStatus(elements.locationStatus, `Erro ao carregar localizações: ${error.message}`, true);
  }
}

async function deleteLocation(id) {
  try {
    await apiFetch(`/api/locations/${id}`, { method: "DELETE", requireAuth: true });
    showStatus(elements.locationStatus, `Localização #${id} eliminada.`, false);
    await loadLocations();
  } catch (error) {
    showStatus(elements.locationStatus, `Erro ao eliminar localização: ${error.message}`, true);
  }
}

async function createLocation(event) {
  event.preventDefault();

  try {
    const location = {
      city: document.getElementById("locationCity").value,
      district: document.getElementById("locationDistrict").value,
      latitude: Number(document.getElementById("locationLatitude").value),
      longitude: Number(document.getElementById("locationLongitude").value),
    };

    const created = await apiFetch("/api/locations", { method: "POST", body: location, requireAuth: true });
    showStatus(elements.locationStatus, `Localização #${created.id} criada.`, false);
    elements.locationForm.reset();
    await loadLocations();
  } catch (error) {
    showStatus(elements.locationStatus, `Erro ao criar localização: ${error.message}`, true);
  }
}

function renderReadings(readings) {
  elements.readingsTableBody.innerHTML = "";

  for (const reading of readings) {
    const locationLabel = reading.location ? reading.location.city : `#${reading.locationId}`;
    const row = document.createElement("tr");
    row.innerHTML = `
      <td>${reading.id}</td>
      <td>${locationLabel}</td>
      <td>${reading.dateTime ?? ""}</td>
      <td>${reading.temperature}</td>
      <td>${reading.humidity}</td>
      <td>${reading.windSpeed}</td>
      <td>${reading.europeanAqi}</td>
      <td class="inline-actions"></td>
    `;

    const deleteButton = document.createElement("button");
    deleteButton.textContent = "Eliminar";
    deleteButton.className = "danger";
    deleteButton.addEventListener("click", () => deleteReading(reading.id));
    row.querySelector(".inline-actions").appendChild(deleteButton);

    elements.readingsTableBody.appendChild(row);
  }
}

async function loadReadings() {
  try {
    const readings = await apiFetch("/api/readings");
    renderReadings(readings);
  } catch (error) {
    showStatus(elements.readingStatus, `Erro ao carregar leituras: ${error.message}`, true);
  }
}

async function deleteReading(id) {
  try {
    await apiFetch(`/api/readings/${id}`, { method: "DELETE", requireAuth: true });
    showStatus(elements.readingStatus, `Leitura #${id} eliminada.`, false);
    await loadReadings();
  } catch (error) {
    showStatus(elements.readingStatus, `Erro ao eliminar leitura: ${error.message}`, true);
  }
}

async function createReading(event) {
  event.preventDefault();

  try {
    const reading = {
      locationId: Number(elements.readingLocation.value),
      dateTime: new Date().toISOString(),
      temperature: Number(document.getElementById("readingTemperature").value),
      humidity: Number(document.getElementById("readingHumidity").value),
      windSpeed: Number(document.getElementById("readingWindSpeed").value),
      precipitation: Number(document.getElementById("readingPrecipitation").value),
      pm10: Number(document.getElementById("readingPm10").value),
      pm25: Number(document.getElementById("readingPm25").value),
      europeanAqi: Number(document.getElementById("readingEuropeanAqi").value),
    };

    const created = await apiFetch("/api/readings", { method: "POST", body: reading, requireAuth: true });
    const alertMessage = created.alert ? ` Alerta gerado: ${created.alert.warningLevel}.` : " Sem alerta gerado.";
    showStatus(elements.readingStatus, `Leitura #${created.reading.id} criada.${alertMessage}`, false);
    elements.readingForm.reset();
    await Promise.all([loadReadings(), loadAlerts()]);
  } catch (error) {
    showStatus(elements.readingStatus, `Erro ao criar leitura: ${error.message}`, true);
  }
}

async function importCurrentReading() {
  try {
    const locationId = elements.readingLocation.value;

    if (!locationId) {
      throw new Error("Seleciona uma localização primeiro.");
    }

    const created = await apiFetch(`/api/readings/import-current/${locationId}`, { method: "POST", requireAuth: true });
    const alertMessage = created.alert ? ` Alerta gerado: ${created.alert.warningLevel}.` : " Sem alerta gerado.";
    showStatus(elements.readingStatus, `Leitura atual importada (#${created.reading.id}).${alertMessage}`, false);
    await Promise.all([loadReadings(), loadAlerts()]);
  } catch (error) {
    showStatus(elements.readingStatus, `Erro ao importar leitura atual: ${error.message}`, true);
  }
}

function renderAlerts(alerts) {
  elements.alertsTableBody.innerHTML = "";

  for (const alert of alerts) {
    const locationLabel = alert.location ? alert.location.city : `#${alert.locationId}`;
    const row = document.createElement("tr");
    row.innerHTML = `
      <td>${alert.id}</td>
      <td>${locationLabel}</td>
      <td>${alert.alertType}</td>
      <td>${alert.warningLevel}</td>
      <td>${alert.message}</td>
      <td>${alert.createdAt ?? ""}</td>
      <td class="inline-actions"></td>
    `;

    const deleteButton = document.createElement("button");
    deleteButton.textContent = "Eliminar";
    deleteButton.className = "danger";
    deleteButton.addEventListener("click", () => deleteAlert(alert.id));
    row.querySelector(".inline-actions").appendChild(deleteButton);

    elements.alertsTableBody.appendChild(row);
  }
}

async function loadAlerts() {
  try {
    const alerts = await apiFetch("/api/alerts");
    renderAlerts(alerts);
  } catch (error) {
    showStatus(elements.alertStatus, `Erro ao carregar alertas: ${error.message}`, true);
  }
}

async function deleteAlert(id) {
  try {
    await apiFetch(`/api/alerts/${id}`, { method: "DELETE", requireAuth: true });
    showStatus(elements.alertStatus, `Alerta #${id} eliminado.`, false);
    await loadAlerts();
  } catch (error) {
    showStatus(elements.alertStatus, `Erro ao eliminar alerta: ${error.message}`, true);
  }
}

async function loadAll() {
  await loadLocations();
  await Promise.all([loadReadings(), loadAlerts()]);
}

elements.saveConnectionButton.addEventListener("click", () => {
  saveConnectionSettings();
  loadAll();
});
elements.healthCheckButton.addEventListener("click", checkHealth);
elements.locationForm.addEventListener("submit", createLocation);
elements.readingForm.addEventListener("submit", createReading);
elements.importCurrentButton.addEventListener("click", importCurrentReading);
elements.refreshAlertsButton.addEventListener("click", loadAlerts);

loadConnectionSettings();

if (getBaseUrl()) {
  loadAll();
}
