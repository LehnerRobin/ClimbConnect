import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Area, AreasService } from '../../../services/areas.service';

@Component({
  selector: 'app-areas-page',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './areas-page.component.html',
  styleUrl: './areas-page.component.css',
})
export class AreasPageComponent implements OnInit {

  private areasService = inject(AreasService);

  areas: Area[] = [];
  loading = true;
  errorMessage = '';
  searchTerm = '';

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
    this.loadAreas(this.searchTerm);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.loadAreas('');
  }

  getInitial(area: Area): string {
    return area.name?.trim().charAt(0).toUpperCase() || '?';
  }
}
