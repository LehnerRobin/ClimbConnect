import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-area-detail-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './area-detail-page.html',
  styleUrl: './area-detail-page.scss',
})
export class AreaDetailPage {
  private route = inject(ActivatedRoute);

  id = this.route.snapshot.paramMap.get('id');



}

