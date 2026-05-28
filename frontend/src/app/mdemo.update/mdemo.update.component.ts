import { Component, OnInit, inject, InputSignal, input, effect, model } from '@angular/core';
import { DataService } from '../../services/data.service';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule, NgModel } from '@angular/forms';

@Component({
  selector: 'app-mdemo-update.component',
  imports: [FormsModule],
  templateUrl: './mdemo.update.component.html',
  styleUrl: './mdemo.update.component.css'
})
export class MDemoUpdateComponent {

  mdemoIdVal = model<number>(0);
  mdemoNameVal = model<string | null>(null);

  id: InputSignal<number | undefined> = input();

  private dataService = inject(DataService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  constructor() {
    effect(() => {
      this.mdemoIdVal.set(this.id()!);
      this.load();
    });
  }

  load() {
    this.dataService.getMDemo(this.mdemoIdVal()).subscribe({
      next: data => {
        this.mdemoNameVal.set(data.name);

      },
      error: error => {
        console.error('Error loading mdemo:', error)
      }
    })
  }

  onUpdateMDemo() {
    this.dataService.updateMDemo({
      id: this.mdemoIdVal(),
      name: this.mdemoNameVal()!,
      fDemoId: 1
    }).subscribe({
      next: () => {
        this.router.navigate(['/mdemos']);
      },
      error: (err) => {
        console.error(err);
      }
    });
  } 
}
