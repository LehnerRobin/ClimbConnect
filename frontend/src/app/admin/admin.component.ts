import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent implements OnInit {

  private adminService = inject(AdminService);

  activeTab: 'areas' | 'sectors' | 'routes' = 'areas';

  // ── Gebiete ────────────────────────────────────────────────────────────────
  areas: any[] = [];
  showAreaForm = false;
  editingArea: any = null;
  areaForm = { name: '', location: '', description: '', imageUrl: '' };

  // ── Sektoren ───────────────────────────────────────────────────────────────
  selectedAreaForSectors: any = null;
  sectors: any[] = [];
  showSectorForm = false;
  editingSector: any = null;
  sectorForm = { name: '', description: '' };

  // ── Routen ─────────────────────────────────────────────────────────────────
  selectedAreaForRoutes: any = null;
  sectorsForRoutes: any[] = [];
  selectedSectorForRoutes: any = null;
  routes: any[] = [];
  showRouteForm = false;
  editingRoute: any = null;
  routeForm = { name: '', grade: '', style: '', lengthMeters: null as number | null, description: '' };

  errorMsg = '';
  successMsg = '';

  readonly styles = ['Sport', 'Trad', 'Boulder', 'Multi-Pitch', 'Klettersteig'];

  ngOnInit(): void {
    this.loadAreas();
  }

  setTab(tab: 'areas' | 'sectors' | 'routes'): void {
    this.activeTab = tab;
    this.clearMessages();
    this.resetForms();
  }

  // ── Gebiete ────────────────────────────────────────────────────────────────

  loadAreas(): void {
    this.adminService.getAreas().subscribe({
      next: (data) => { this.areas = data; },
      error: () => { this.errorMsg = 'Gebiete konnten nicht geladen werden.'; }
    });
  }

  startCreateArea(): void {
    this.editingArea = null;
    this.areaForm = { name: '', location: '', description: '', imageUrl: '' };
    this.showAreaForm = true;
  }

  startEditArea(area: any): void {
    this.editingArea = area;
    this.areaForm = {
      name: area.name ?? '',
      location: area.location ?? '',
      description: area.description ?? '',
      imageUrl: area.imageUrl ?? ''
    };
    this.showAreaForm = true;
  }

  saveArea(): void {
    if (!this.areaForm.name.trim()) { this.errorMsg = 'Name ist erforderlich.'; return; }
    this.clearMessages();

    const payload = {
      name:        this.areaForm.name.trim(),
      location:    this.areaForm.location.trim() || null,
      description: this.areaForm.description.trim() || null,
      imageUrl:    this.areaForm.imageUrl.trim() || null
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

  deleteArea(area: any): void {
    if (!confirm(`Gebiet "${area.name}" wirklich löschen? Alle zugehörigen Sektoren und Routen werden ebenfalls gelöscht.`)) return;
    this.adminService.deleteArea(area.id).subscribe({
      next: () => { this.successMsg = 'Gebiet gelöscht.'; this.loadAreas(); },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Löschen.'; }
    });
  }

  // ── Sektoren ───────────────────────────────────────────────────────────────

  selectAreaForSectors(area: any): void {
    this.selectedAreaForSectors = area;
    this.showSectorForm = false;
    this.editingSector = null;
    this.clearMessages();
    this.adminService.getSectorsByArea(area.id).subscribe({
      next: (data) => { this.sectors = data; },
      error: () => { this.errorMsg = 'Sektoren konnten nicht geladen werden.'; }
    });
  }

  startCreateSector(): void {
    this.editingSector = null;
    this.sectorForm = { name: '', description: '' };
    this.showSectorForm = true;
  }

  startEditSector(sector: any): void {
    this.editingSector = sector;
    this.sectorForm = { name: sector.name ?? '', description: sector.description ?? '' };
    this.showSectorForm = true;
  }

  saveSector(): void {
    if (!this.sectorForm.name.trim()) { this.errorMsg = 'Name ist erforderlich.'; return; }
    this.clearMessages();

    const payload = {
      name:        this.sectorForm.name.trim(),
      description: this.sectorForm.description.trim() || null
    };

    const req = this.editingSector
      ? this.adminService.updateSector(this.editingSector.id, payload)
      : this.adminService.createSector(this.selectedAreaForSectors.id, payload);

    req.subscribe({
      next: () => {
        this.successMsg = this.editingSector ? 'Sektor aktualisiert.' : 'Sektor erstellt.';
        this.showSectorForm = false;
        this.selectAreaForSectors(this.selectedAreaForSectors);
      },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Speichern.'; }
    });
  }

  deleteSector(sector: any): void {
    if (!confirm(`Sektor "${sector.name}" wirklich löschen? Alle zugehörigen Routen werden ebenfalls gelöscht.`)) return;
    this.adminService.deleteSector(sector.id).subscribe({
      next: () => {
        this.successMsg = 'Sektor gelöscht.';
        this.selectAreaForSectors(this.selectedAreaForSectors);
      },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Löschen.'; }
    });
  }

  // ── Routen ─────────────────────────────────────────────────────────────────

  selectAreaForRoutes(area: any): void {
    this.selectedAreaForRoutes = area;
    this.selectedSectorForRoutes = null;
    this.routes = [];
    this.showRouteForm = false;
    this.clearMessages();
    this.adminService.getSectorsByArea(area.id).subscribe({
      next: (data) => { this.sectorsForRoutes = data; },
      error: () => { this.errorMsg = 'Sektoren konnten nicht geladen werden.'; }
    });
  }

  selectSectorForRoutes(sector: any): void {
    this.selectedSectorForRoutes = sector;
    this.showRouteForm = false;
    this.editingRoute = null;
    this.clearMessages();
    this.adminService.getRoutesBySector(sector.id).subscribe({
      next: (data) => { this.routes = data; },
      error: () => { this.errorMsg = 'Routen konnten nicht geladen werden.'; }
    });
  }

  startCreateRoute(): void {
    this.editingRoute = null;
    this.routeForm = { name: '', grade: '', style: 'Sport', lengthMeters: null, description: '' };
    this.showRouteForm = true;
  }

  startEditRoute(route: any): void {
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
    if (!this.routeForm.grade.trim()) { this.errorMsg = 'Grad ist erforderlich (franz. Skala, z. B. 6a).'; return; }
    this.clearMessages();

    const payload = {
      name:         this.routeForm.name.trim(),
      grade:        this.routeForm.grade.trim().toLowerCase(),
      style:        this.routeForm.style.trim() || null,
      lengthMeters: this.routeForm.lengthMeters ?? null,
      description:  this.routeForm.description.trim() || null
    };

    const req = this.editingRoute
      ? this.adminService.updateRoute(this.editingRoute.id, payload)
      : this.adminService.createRoute(this.selectedSectorForRoutes.id, payload);

    req.subscribe({
      next: () => {
        this.successMsg = this.editingRoute ? 'Route aktualisiert.' : 'Route erstellt.';
        this.showRouteForm = false;
        this.selectSectorForRoutes(this.selectedSectorForRoutes);
      },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Speichern.'; }
    });
  }

  deleteRoute(route: any): void {
    if (!confirm(`Route "${route.name}" wirklich löschen?`)) return;
    this.adminService.deleteRoute(route.id).subscribe({
      next: () => {
        this.successMsg = 'Route gelöscht.';
        this.selectSectorForRoutes(this.selectedSectorForRoutes);
      },
      error: (err) => { this.errorMsg = err?.error?.error ?? 'Fehler beim Löschen.'; }
    });
  }

  // ── Datenbank zurücksetzen ─────────────────────────────────────────────────

  initDatabase(): void {
    if (!confirm('Wirklich alles löschen und Datenbank neu initialisieren?')) return;
    this.adminService.initDatabase().subscribe({
      next: () => { this.successMsg = 'Datenbank zurückgesetzt.'; this.loadAreas(); },
      error: () => { this.errorMsg = 'Fehler beim Zurücksetzen.'; }
    });
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
