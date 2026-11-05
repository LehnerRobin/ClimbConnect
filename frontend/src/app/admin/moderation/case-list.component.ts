import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ModerationService } from '../../../services/moderation.service';
import { ModerationCaseSummary } from '../../../models/pinnwand.model';
import { daysUntil } from './moderation-utils';

/** Admin-Tab "Pinnwand-Meldungen": Liste aller offenen Fälle, neueste Meldung zuerst. */
@Component({
  selector: 'app-case-list',
  standalone: true,
  imports: [DatePipe, RouterLink],
  templateUrl: './case-list.component.html',
  styleUrl: './moderation.css'
})
export class CaseListComponent implements OnInit {

  private moderationService = inject(ModerationService);

  cases: ModerationCaseSummary[] = [];
  loading = true;
  error = '';

  readonly daysUntil = daysUntil;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.moderationService.getCases().subscribe({
      next: cases => {
        this.cases = [...cases].sort((a, b) => b.lastReportAtUtc.localeCompare(a.lastReportAtUtc));
        this.loading = false;
      },
      error: () => {
        this.error = 'Fälle konnten nicht geladen werden.';
        this.loading = false;
      }
    });
  }
}
