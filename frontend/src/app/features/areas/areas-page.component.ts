import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Area, AreasService } from '../../../services/areas.service';
import { FavoritesService } from '../../../services/favorites.service';
import { AreaMapComponent } from './area-map.component';

@Component({
  selector: 'app-areas-page',
  standalone: true,
  imports: [RouterLink, FormsModule, CommonModule, AreaMapComponent],
  templateUrl: './areas-page.component.html',
  styleUrl: './areas-page.component.css',
})
export class AreasPageComponent implements OnInit {

  private areasService = inject(AreasService);
  favoritesService = inject(FavoritesService);

  showOnlyFavorites = false;

  areas: Area[] = [];
  loading = true;
  errorMessage = '';
  searchTerm = '';
  private searchDebounce?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    this.loadAreas();
  }

  get totalVisitorsToday(): number {
    return this.areas.reduce((sum, area) => sum + (area.todayVisitors ?? 0), 0);
  }

  get totalAppointmentsToday(): number {
    return this.areas.reduce((sum, area) => sum + (area.todayAppointments ?? 0), 0);
  }

  loadAreas(search = this.searchTerm): void {
    this.loading = true;
    this.errorMessage = '';

    const trimmedSearch = search.trim();

    this.areasService.getAreas(trimmedSearch || undefined).subscribe({
      next: (areas) => {
        this.areas = areas;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Gebiete konnten nicht geladen werden.';
        this.loading = false;
      }
    });
  }

  searchAreas(): void {
    this.cancelSearchDebounce();
    this.loadAreas(this.searchTerm);
  }

  onSearchInput(value: string): void {
    this.searchTerm = value;
    this.cancelSearchDebounce();

    this.searchDebounce = setTimeout(() => {
      this.loadAreas(this.searchTerm);
    }, 350);
  }

  clearSearch(): void {
    this.cancelSearchDebounce();
    this.searchTerm = '';
    this.loadAreas('');
  }

  private cancelSearchDebounce(): void {
    if (this.searchDebounce) {
      clearTimeout(this.searchDebounce);
      this.searchDebounce = undefined;
    }
  }

  getInitial(area: Area): string {
    return area.name?.trim().charAt(0).toUpperCase() || '?';
  }

  /** Gibt das echte Bild zurück oder wählt eine lokale SVG-Illustration als Fallback. */
  getAreaImage(area: Area): string {
    if (area.imageUrl) return area.imageUrl;
    const imgs = [
      '/assets/areas/area-alpine.svg',
      '/assets/areas/area-wall.svg',
      '/assets/areas/area-ridge.svg'
    ];
    return imgs[area.id % imgs.length];
  }
  /** Fallback auf lokale SVG-Illustration wenn imageUrl nicht geladen werden kann. */
  onImageError(event: Event, area: { id: number }): void {
    const img = event.target as HTMLImageElement;
    const imgs = [
      '/assets/areas/area-alpine.svg',
      '/assets/areas/area-wall.svg',
      '/assets/areas/area-ridge.svg'
    ];
    // Verhindern dass der Fehler-Handler sich selbst aufruft wenn die SVG auch nicht lädt
    img.onerror = null;
    img.src = imgs[area.id % imgs.length];
  }


  get displayedAreas(): Area[] {
    if (!this.showOnlyFavorites) return this.areas;
    return this.areas.filter(a => this.favoritesService.isFavorite(a.id));
  }

  toggleFavorite(event: Event, areaId: number): void {
    event.preventDefault();
    event.stopPropagation();
    this.favoritesService.toggle(areaId);
  }
}
