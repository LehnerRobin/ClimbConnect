import { CommonModule } from '@angular/common';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { Component, OnInit, inject } from '@angular/core';
import { MDemo } from '../../models/mdemo.model';
import { DataService } from '../../services/data.service';

@Component({
  selector: 'app-mdemo.add',
  templateUrl: './mdemo.add.component.html',
  styleUrls: ['./mdemo.add.component.css'],
  imports: [CommonModule, FormsModule],
})
export class MDemoAddComponent implements OnInit {

  mDemoNameVal: string = '';
  ageVal: number | undefined;
  minPlayersVal: number | null = null;
  maxPlayersVal: number | null = null;

  private dataService = inject(DataService);
  private router = inject(Router);
  
  constructor() {
  }

  ngOnInit(): void {
    console.log('MDemoDetailComponent initialized');
  }

  onAddMDemo(): void {
    this.dataService.addMDemo(this.mDemoNameVal, this.ageVal!, this.minPlayersVal, this.maxPlayersVal).subscribe({
      next: (data) => {
        this.router.navigate(['/demos']);
      },
      error: (error) => {
        console.log(error);
        alert('Create mDemo failed');
      }
    });
  }
}