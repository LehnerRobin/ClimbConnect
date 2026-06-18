import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Area, AreasService } from '../../../services/areas.service';

@Component({
  selector: 'app-area-detail',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './area-detail.component.html',
  styleUrl: './area-detail.component.css'
})
export class AreaDetailComponent implements OnInit {

  private route = inject(ActivatedRoute);
  private areasService = inject(AreasService);

  area?: Area;
  loading = true;
  errorMessage = '';

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!id) {
      this.errorMessage = 'Ungültige Area-ID.';
      this.loading = false;
      return;
    }

    this.areasService.getAreaById(id).subscribe({
      next: (area) => {
        this.area = area;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Gebiet konnte nicht geladen werden.';
        this.loading = false;
      }
    });
  }
}