import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AreasApiService } from '../areas-api';


@Component({
  selector: 'app-areas-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './areas-page.html',
})
export class AreasPage {
  areas: any[] = [];

  constructor(private api: AreasApiService) {
    this.api.getAreas().subscribe((data: any[]) => {
      this.areas = data.slice(0, 5);
      console.log('API areas:', this.areas);
    });
  }
}
