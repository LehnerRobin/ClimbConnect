import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { PageBandComponent } from '../shared/page-band.component';

@Component({
  selector: 'app-forbidden',
  imports: [RouterModule, PageBandComponent],
  templateUrl: './forbidden.component.html',
  styleUrls: ['./forbidden.component.css']
})
export class ForbiddenComponent {}
