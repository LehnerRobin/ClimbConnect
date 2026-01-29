import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AreasApiService } from '../areas-api.service';
import type { AreaDetail } from '../areas-api';


@Component({
  selector: 'app-area-detail-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './area-detail-page.html',
  styleUrl: './area-detail-page.scss',
})
export class AreaDetailPage implements OnInit {
  area: AreaDetail | null = null;
  loading = true;
  error: string | null = null;

  constructor(private route: ActivatedRoute, private api: AreasApiService) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error = 'Ungültige ID';
      this.loading = false;
      return;
    }

    this.api.getArea(id).subscribe({
      next: (data) => {
        this.area = data;
        this.loading = false;
      },
      error: () => {
        this.error = 'Konnte Area-Detail nicht laden.';
        this.loading = false;
      },
    });
  }
}
