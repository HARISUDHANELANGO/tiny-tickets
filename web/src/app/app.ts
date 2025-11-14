import { Component, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { environment } from '../environments/environment.prod';

type Ticket = { id: number; title: string };

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
})
export class AppComponent {
  private http = inject(HttpClient);
  apiBase = environment.apiUrl;
  tickets: Ticket[] = [];
  title = '';

  ngOnInit() {
    this.load();
  }

  load() {
    this.http.get<Ticket[]>(`${this.apiBase}/tickets`)
      .subscribe(x => this.tickets = x);
  }

  create() {
    if (!this.title.trim()) return;
    this.http.post(`${this.apiBase}/tickets`, { title: this.title })
      .subscribe(() => {
        this.title = '';
        this.load();
      });
  }

  publish(id: number) {
    this.http.post(`${this.apiBase}/tickets/${id}/publish`, {})
      .subscribe(() => {
        console.log("Event published from UI");
      });
  }

}
