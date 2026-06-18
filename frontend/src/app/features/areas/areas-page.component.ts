import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Area, AreasService } from '../../../services/areas.service';

@Component({
  selector: 'app-areas-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './areas-page.component.html',
  styleUrl: './areas-page.component.css',
})
export class AreasPageComponent implements OnInit {

  private areasService = inject(AreasService);

  areas: Area[] = [];
  loading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadAreas();
  }

  loadAreas(): void {
    this.loading = true;
    this.errorMessage = '';

    this.areasService.getAreas().subscribe({
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
}