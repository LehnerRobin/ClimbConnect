import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

import { Component, OnInit, inject } from '@angular/core';
import { MDemo, MDemoOverview } from '../../models/mdemo.model';
import { DataService } from '../../services/data.service';

@Component({
  selector: 'app-mdemos',
  templateUrl: './mdemos.component.html',
  styleUrls: ['./mdemos.component.css'],
  imports: [CommonModule],
})
export class MDemosComponent implements OnInit {

  mdemos: MDemoOverview[] = [];

  private dataService = inject(DataService);
  private router = inject(Router);


  constructor() {
  }
  
  ngOnInit(): void {
    console.log('MDemosComponent initialized');

    this.dataService.getMDemos().subscribe({
      next: data=>{
        this.mdemos = data;
      },
      error: error=>{
        console.error('Error loading mdemos:', error);
      }
    });
  }

  showMDemo(mdemo: MDemoOverview): void {
    this.router.navigate(['/mdemo-detail',mdemo.id]);
  }  

  addMDemo(): void {
    this.router.navigate(['/mdemo-add']);
  }

  editMDemo(mdemo: MDemoOverview) {
    this.router.navigate(['/mdemo-update', mdemo.id]);
  }
  
  deleteMDemo(mdemo: MDemoOverview): void {
    if(confirm(`Do you really want to delete the mdemo "${mdemo.name}"?`)) {
      this.dataService.deleteMDemo(mdemo.id).subscribe({
        next: () => {
          this.ngOnInit();
        },
        error: error => {
          console.error('Error deleting mdemo:', error);
        }
      });
    }
  }
}

