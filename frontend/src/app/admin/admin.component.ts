import { Component, inject } from '@angular/core';
import { AdminService } from '../../services/admin.service';
@Component({
  selector: 'app-admin',
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent {

  private adminService = inject(AdminService);

  initDatabase(): void {
    if (confirm(`Do you really want to delete everything and re-initialize the database?`)) {
      this.adminService.initDatabase().subscribe({
        next: () => { },
        error: error => {
          console.error('Error deleting:');
        }
      });
    }
  }
}
