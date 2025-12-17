import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AreasApiService, Area } from '../areas-api';

@Component({
  selector: 'app-areas-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './areas-page.html',
  styleUrl: './areas-page.scss',
})
export class AreasPage implements OnInit {
  areas: Area[] = [];
  loading = false;
  error: string | null = null;

  constructor(private api: AreasApiService) {}

  ngOnInit(): void {
    this.loadAreas();
  }

  loadAreas(): void {
    this.loading = true;
    this.error = null;

    this.api.getAreas().subscribe({
      next: (data) => {
        this.areas = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'API Fehler (siehe Console/Network)';
        console.error(err);
        this.loading = false;
      },
    });
  }
}
