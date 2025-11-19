import { Component, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { environment } from '../environments/environment.prod';
import { RouterOutlet } from '@angular/router';

export interface UploadedFile {
  id: number;
  fileName: string;
  blobName: string;
  contentType: string;
  size: number;
  uploadedOn: string;
  url: string;
}

type Ticket = { id: number; title: string };

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterOutlet],
  templateUrl: './app.html',
})
export class AppComponent {
  private http = inject(HttpClient);
  apiBase = environment.apiUrl;

  tickets: Ticket[] = [];
  title = '';

  files: UploadedFile[] = [];
  selectedFile: File | null = null;

  ngOnInit() {
    this.loadTickets();
    this.loadFiles();
  }

  // -------------------------------
  // TICKETS
  // -------------------------------
  loadTickets() {
    this.http
      .get<Ticket[]>(`${this.apiBase}/tickets`)
      .subscribe((res) => (this.tickets = res));
  }

  create() {
    if (!this.title.trim()) return;

    this.http
      .post(`${this.apiBase}/tickets`, { title: this.title })
      .subscribe(() => {
        this.title = '';
        this.loadTickets();
      });
  }

  publish(id: number) {
    this.http
      .post(`${this.apiBase}/tickets/${id}/publish`, {})
      .subscribe(() => console.log('Event published from UI'));
  }

  // -------------------------------
  // FILE UPLOAD
  // -------------------------------
  onFileSelected(e: any) {
    const file = e.target.files?.[0];
    if (!file) return;

    this.selectedFile = file;
    this.uploadFile();
  }

  uploadFile() {
    if (!this.selectedFile) return;

    const guid = crypto.randomUUID();
    const blobName = `${guid}_${this.selectedFile.name}`;
    const container = 'ticket-images';

    // 1 — Ask backend for SAS URL
    this.http
      .post<{ uploadUrl: string }>(`${this.apiBase}/storage/sas-upload`, {
        container,
        fileName: blobName,
      })
      .subscribe(async (res) => {
        // 2 — Upload file using SAS
        await fetch(res.uploadUrl, {
          method: 'PUT',
          headers: { 'x-ms-blob-type': 'BlockBlob' },
          body: this.selectedFile,
        });

        // 3 — Save metadata
        const meta = {
          fileName: this.selectedFile!.name,
          blobName,
          contentType: this.selectedFile!.type,
          size: this.selectedFile!.size,
          container,
        };

        this.http
          .post(`${this.apiBase}/storage/save-metadata`, meta)
          .subscribe(() => this.loadFiles());
      });
  }

  // -------------------------------
  // FILE LIST
  // -------------------------------
  loadFiles() {
    this.http
      .get<UploadedFile[]>(`${this.apiBase}/storage/files`)
      .subscribe((res) => (this.files = res));
  }

  delete(id: number) {
    if (!confirm('Delete this file permanently?')) return;

    this.http.delete(`${this.apiBase}/storage/files/${id}`).subscribe(() => {
      this.files = this.files.filter((f) => f.id !== id);
    });
  }

  downloadReport() {
    this.http
      .get(`${this.apiBase}/storage/files/report/pdf`, { responseType: 'blob' })
      .subscribe((blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'TinyTickets_File_Report.pdf';
        a.click();
        URL.revokeObjectURL(url);
      });
  }
}
