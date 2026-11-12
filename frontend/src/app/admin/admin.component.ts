import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminReport, AdminService, AreaSaveRequest, RouteSaveRequest, SectorSaveRequest } from '../../services/admin.service';
import { Area, AreasService, ClimbingRoute, Sector } from '../../services/areas.service';
import { Grade, GradeService, gradeLabel } from '../../services/grade.service';
import { ActivatedRoute } from '@angular/router';
import { CaseListComponent } from './moderation/case-list.component';
import { PageBandComponent } from '../shared/page-band.component';

type AdminTab = 'areas' | 'sectors' | 'routes' | 'reports' | 'moderation';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, CaseListComponent, PageBandComponent],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent implements OnInit {

  private adminService = inject(AdminService);
  private areasService = inject(AreasService);
  private gradeService = inject(GradeService);
  private route = inject(ActivatedRoute);

  /** Grad-Tabelle aus dem Backend (GET /api/grades) für die Grad-Auswahl bei Routen. */
  grades: Grade[] = [];
  readonly gradeLabel = gradeLabel;

  activeTab: AdminTab = 'areas';

  // ── Gebiete ────────────────────────────────────────────────────────────────
  areas: Area[] = [];
  showAreaForm = false;
  editingArea: Area | null = null;
  areaForm = { name: '', location: '', description: '', imageUrl: '', latitude: null as number | null, longitude: null as number | null };

  // ── Sektoren ───────────────────────────────────────────────────────────────
  selectedAreaForSectors: Area | null = null;
  sectors: Sector[] = [];
  showSectorForm = false;
  editingSector: Sector | null = null;
  sectorForm = { name: '', description: '' };

  // ── Routen ─────────────────────────────────────────────────────────────────
  selectedAreaForRoutes: Area | null = null;
  sectorsForRoutes: Sector[] = [];
  selectedSectorForRoutes: Sector | null = null;
  routes: ClimbingRoute[] = [];
  showRouteForm = false;
  editingRoute: ClimbingRoute | null = null;
  routeForm = { name: '', grade: '', style: '', lengthMeters: null as number | null, description: '' };

  errorMsg = '';
  successMsg = '';

  readonly styles = ['Sport', 'Trad', 'Boulder', 'Multi-Pitch', 'Klettersteig'];

  ngOnInit(): void {
    // ?tab=moderation öffnet direkt einen Tab (z. B. nach "Fall erledigen")
    const tab = this.route.snapshot.queryParamMap.get('tab') as AdminTab | null;
    if (tab && ['areas', 'sectors', 'routes', 'reports', 'moderation'].includes(tab)) {
      this.setTab(tab);
    }
    this.loadAreas();
    this.gradeService.getGrades().subscribe({
      next: grades => this.grades = grades,
      error: () => this.errorMsg = 'Grad-Liste konnte nicht geladen werden.'
    });
  }

  setTab(tab: AdminTab): void {
    this.activeTab = tab;
    this.clearMessages();
    this.resetForms();
    if (tab === 'reports') this.loadReports();
  }

  // ── Gebiete ────────────────────────────────────────────────────────────────

  loadAreas(): void {
    this.areasService.getAreas().subscribe({
      next: (data) => { this.areas = data; },
      error: () => { this.errorMsg = 'Gebiete konnten nicht geladen werden.'; }
    });
  }

  startCreateArea(): void {
    this.editingArea = null;
    this.areaForm = { name: '', location: '', description: '', imageUrl: '', latitude: null as number | null, longitude: null as number | null };
    this.showAreaForm = true;
  }

  startEditArea(area: Area): void {
    this.editingArea = area;
    this.areaForm = {
      name: area.name ?? '',
      location: area.location ?? '',
      description: area.description ?? '',
      imageUrl: area.imageUrl ?? '',
      // Koordinaten übernehmen, sonst würde das Speichern sie löschen (PUT ersetzt alle Felder)
      latitude: area.latitude ?? null,
      longitude: area.longitude ?? null
    };
    this.showAreaForm = true;
  }

  saveArea(): void {
    if (!this.areaForm.name.trim()) { this.errorMsg = 'Name ist erforderlich.'; return; }
    const coordinateError = this.validateCoordinates(this.areaForm.latitude, this.areaForm.longitude);
    if (coordinateError) { this.errorMsg = coordinateError; return; }
    this.clearMessages();

    const payload: AreaSaveRequest = {
      name:        this.areaForm.name.trim(),
      location:    this.areaForm.location.trim() || null,
      description: this.areaForm.description.trim() || null,
      imageUrl:    this.areaForm.imageUrl.trim() || null,
      latitude:    this.areaForm.latitude,
      longitude:   this.areaForm.longitude
    };

    const req = this.editingArea
      ? this.adminService.updateArea(this.editingArea.id, payload)
      : this.adminService.createArea(payload);

    req.subscribe({
      next: () => {
        this.successMsg = this.editingArea ? 'Gebiet aktualisiert.' : 'Gebiet erstellt.';
        this.showAreaForm = false;
        this.loadAreas();
      },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Speichern.'; }
    });
  }

  /**
   * Prüft die Koordinaten: entweder beide leer (Gebiet erscheint nicht auf der Karte)
   * oder beide gesetzt und im gültigen Bereich. Gibt die Fehlermeldung zurück oder null.
   */
  private validateCoordinates(latitude: number | null, longitude: number | null): string | null {
    if (latitude == null && longitude == null) return null;
    if (latitude == null || longitude == null) {
      return 'Bitte Breiten- und Längengrad beide angeben oder beide leer lassen.';
    }
    if (latitude < -90 || latitude > 90)     return 'Der Breitengrad muss zwischen -90 und 90 liegen.';
    if (longitude < -180 || longitude > 180) return 'Der Längengrad muss zwischen -180 und 180 liegen.';
    return null;
  }

  deleteArea(area: Area): void {
    if (!confirm(`Gebiet "${area.name}" wirklich löschen? Alle zugehörigen Sektoren und Routen werden ebenfalls gelöscht.`)) return;
    this.adminService.deleteArea(area.id).subscribe({
      next: () => { this.successMsg = 'Gebiet gelöscht.'; this.loadAreas(); },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Löschen.'; }
    });
  }

  // ── Sektoren ───────────────────────────────────────────────────────────────

  /** Wird auch mit undefined aufgerufen, wenn im Auswahlfeld „— Gebiet wählen —" gewählt wird. */
  selectAreaForSectors(area: Area | undefined): void {
    this.selectedAreaForSectors = area ?? null;
    this.showSectorForm = false;
    this.editingSector = null;
    this.clearMessages();
    if (!area) { this.sectors = []; return; }
    this.areasService.getSectorsByArea(area.id).subscribe({
      next: (data) => { this.sectors = data; },
      error: () => { this.errorMsg = 'Sektoren konnten nicht geladen werden.'; }
    });
  }

  startCreateSector(): void {
    this.editingSector = null;
    this.sectorForm = { name: '', description: '' };
    this.showSectorForm = true;
  }

  startEditSector(sector: Sector): void {
    this.editingSector = sector;
    this.sectorForm = { name: sector.name ?? '', description: sector.description ?? '' };
    this.showSectorForm = true;
  }

  saveSector(): void {
    if (!this.sectorForm.name.trim()) { this.errorMsg = 'Name ist erforderlich.'; return; }
    const area = this.selectedAreaForSectors;
    if (!area) return;
    this.clearMessages();

    const payload: SectorSaveRequest = {
      name:        this.sectorForm.name.trim(),
      description: this.sectorForm.description.trim() || null
    };

    const req = this.editingSector
      ? this.adminService.updateSector(this.editingSector.id, payload)
      : this.adminService.createSector(area.id, payload);

    req.subscribe({
      next: () => {
        this.successMsg = this.editingSector ? 'Sektor aktualisiert.' : 'Sektor erstellt.';
        this.showSectorForm = false;
        this.selectAreaForSectors(area);
      },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Speichern.'; }
    });
  }

  deleteSector(sector: Sector): void {
    if (!confirm(`Sektor "${sector.name}" wirklich löschen? Alle zugehörigen Routen werden ebenfalls gelöscht.`)) return;
    const area = this.selectedAreaForSectors;
    this.adminService.deleteSector(sector.id).subscribe({
      next: () => {
        this.successMsg = 'Sektor gelöscht.';
        this.selectAreaForSectors(area ?? undefined);
      },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Löschen.'; }
    });
  }

  // ── Routen ─────────────────────────────────────────────────────────────────

  /** Wird auch mit undefined aufgerufen, wenn im Auswahlfeld „— Gebiet wählen —" gewählt wird. */
  selectAreaForRoutes(area: Area | undefined): void {
    this.selectedAreaForRoutes = area ?? null;
    this.selectedSectorForRoutes = null;
    this.sectorsForRoutes = [];
    this.routes = [];
    this.showRouteForm = false;
    this.clearMessages();
    if (!area) return;
    this.areasService.getSectorsByArea(area.id).subscribe({
      next: (data) => { this.sectorsForRoutes = data; },
      error: () => { this.errorMsg = 'Sektoren konnten nicht geladen werden.'; }
    });
  }

  /**
   * Wird auch mit undefined aufgerufen, wenn „— Sektor wählen —" gewählt wird.
   * Routen immer in französischer Skala laden: So wird der Grad gespeichert und geprüft.
   */
  selectSectorForRoutes(sector: Sector | undefined): void {
    this.selectedSectorForRoutes = sector ?? null;
    this.showRouteForm = false;
    this.editingRoute = null;
    this.clearMessages();
    if (!sector) { this.routes = []; return; }
    this.areasService.getRoutesBySector(sector.id, 'french').subscribe({
      next: (data) => { this.routes = data; },
      error: () => { this.errorMsg = 'Routen konnten nicht geladen werden.'; }
    });
  }

  startCreateRoute(): void {
    this.editingRoute = null;
    this.routeForm = { name: '', grade: '', style: 'Sport', lengthMeters: null, description: '' };
    this.showRouteForm = true;
  }

  startEditRoute(route: ClimbingRoute): void {
    this.editingRoute = route;
    this.routeForm = {
      name:         route.name ?? '',
      grade:        route.grade ?? '',
      style:        route.style ?? 'Sport',
      lengthMeters: route.lengthMeters ?? null,
      description:  route.description ?? ''
    };
    this.showRouteForm = true;
  }

  saveRoute(): void {
    if (!this.routeForm.name.trim()) { this.errorMsg = 'Name ist erforderlich.'; return; }
    if (!this.routeForm.grade.trim()) { this.errorMsg = 'Bitte einen Grad auswählen.'; return; }
    const sector = this.selectedSectorForRoutes;
    if (!sector) return;
    this.clearMessages();

    const payload: RouteSaveRequest = {
      name:         this.routeForm.name.trim(),
      grade:        this.routeForm.grade.trim().toLowerCase(),
      style:        this.routeForm.style.trim() || null,
      lengthMeters: this.routeForm.lengthMeters ?? null,
      description:  this.routeForm.description.trim() || null
    };

    const req = this.editingRoute
      ? this.adminService.updateRoute(this.editingRoute.id, payload)
      : this.adminService.createRoute(sector.id, payload);

    req.subscribe({
      next: () => {
        this.successMsg = this.editingRoute ? 'Route aktualisiert.' : 'Route erstellt.';
        this.showRouteForm = false;
        this.selectSectorForRoutes(sector);
      },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Speichern.'; }
    });
  }

  deleteRoute(route: ClimbingRoute): void {
    if (!confirm(`Route "${route.name}" wirklich löschen?`)) return;
    const sector = this.selectedSectorForRoutes;
    this.adminService.deleteRoute(route.id).subscribe({
      next: () => {
        this.successMsg = 'Route gelöscht.';
        this.selectSectorForRoutes(sector ?? undefined);
      },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Löschen.'; }
    });
  }

  // ── Reports ────────────────────────────────────────────────────────────────

  reports: AdminReport[] = [];
  showResolvedReports = false;

  loadReports(): void {
    this.clearMessages();
    this.adminService.getReports().subscribe({
      next: (data) => { this.reports = data; },
      error: () => { this.errorMsg = 'Reports konnten nicht geladen werden.'; }
    });
  }

  resolveReport(report: AdminReport): void {
    this.clearMessages();
    this.adminService.resolveReport(report.id).subscribe({
      next: () => {
        report.status = 'Resolved';
        this.successMsg = 'Report als erledigt markiert.';
      },
      error: () => { this.errorMsg = 'Fehler beim Aktualisieren des Reports.'; }
    });
  }

  get visibleReports(): AdminReport[] {
    return this.showResolvedReports
      ? this.reports
      : this.reports.filter(r => r.status !== 'Resolved');
  }

  severityClass(severity: string): string {
    switch (severity?.toLowerCase()) {
      case 'high':   return 'severity-high';
      case 'medium': return 'severity-medium';
      default:       return 'severity-low';
    }
  }

  private clearMessages(): void { this.errorMsg = ''; this.successMsg = ''; }

  private resetForms(): void {
    this.showAreaForm = false;
    this.showSectorForm = false;
    this.showRouteForm = false;
    this.editingArea = null;
    this.editingSector = null;
    this.editingRoute = null;
  }
}
