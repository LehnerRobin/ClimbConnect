import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent {

  user = {
    username: '',
    email: '',
    bio: '',
    preferredGradeScale: 'UIAA',
    averageGrade: ''
  };

}