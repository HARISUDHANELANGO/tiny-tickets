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
  selectedFile!: File;

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

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      this.uploadFile();
    }
  }

  uploadFile() {
    if (!this.selectedFile) return;

    const fileName = `${crypto.randomUUID()}_${this.selectedFile.name}`;

    // 1 — Get SAS URL from API
    this.http.post<any>(`${this.apiBase}/storage/sas-upload`, {
      container: "temp",
      fileName: fileName
    }).subscribe(async (res) => {
      const sasUrl = res.uploadUrl;

      // 2 — Upload directly to blob storage
      await fetch(sasUrl, {
        method: "PUT",
        headers: {
          "x-ms-blob-type": "BlockBlob"
        },
        body: this.selectedFile
      });

      console.log("File uploaded:", sasUrl.split("?")[0]);
      alert("Upload Successful!");
    });
  }

}
