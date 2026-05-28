import { CommonModule } from '@angular/common';
import { ActivatedRoute, Params, Router } from '@angular/router';

import { Component, OnInit, InputSignal, input, inject, effect } from '@angular/core';
import { MDemo } from '../../models/mdemo.model';
import { DataService } from '../../services/data.service';

@Component({
  selector: 'app-mdemo.detail',
  templateUrl: './mdemo.detail.component.html',
  styleUrls: ['./mdemo.detail.component.css'],
  imports: [CommonModule],
})
export class MDemoDetailComponent {

  mdemo: MDemo | undefined;
  id: InputSignal<number | undefined> = input();

  private dataService = inject(DataService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  constructor() {
    effect(() => {
      this.load();
    });
  }

  load(): void {
    this.dataService.getMDemo(this.id()!).subscribe({
      next: data => {
        this.mdemo = data;
      },
      error: error => {
        console.error('Error loading mdemo:', error);
      }
    });
  }
}