import { Component, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { environment } from '../environments/environment.prod';

type Ticket = { id: number; title: string };

export interface UploadedFile {
  id: number;
  fileName: string;
  blobName: string;
  contentType: string;
  uploadedOn: string;
  url: string;
}

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

  files: UploadedFile[] = [];
  selectedFile: File | null = null;

  previewUrl = '';

  ngOnInit() {
    this.loadTickets();
    this.loadFiles();
  }

  // -------------------------------
  // TICKETS
  // -------------------------------

  loadTickets() {
    this.http.get<Ticket[]>(`${this.apiBase}/tickets`)
      .subscribe(res => this.tickets = res);
  }

  create() {
    if (!this.title.trim()) return;

    this.http.post(`${this.apiBase}/tickets`, { title: this.title })
      .subscribe(() => {
        this.title = '';
        this.loadTickets();
      });
  }

  publish(id: number) {
    this.http.post(`${this.apiBase}/tickets/${id}/publish`, {})
      .subscribe(() => console.log("Event published from UI"));
  }

  // -------------------------------
  // FILE UPLOAD + LIST
  // -------------------------------

  onFileSelected(event: any) {
    const file = event.target.files?.[0];
    if (!file) return;

    this.selectedFile = file;
    this.uploadFile();
  }

  uploadFile() {
    if (!this.selectedFile) return;

    const blobName = `${crypto.randomUUID()}_${this.selectedFile.name}`;
    const container = "ticket-images";

    // 1 — Ask backend for SAS upload URL
    this.http.post<{ uploadUrl: string }>(
      `${this.apiBase}/storage/sas-upload`,
      { container, fileName: blobName }
    )
    .subscribe(async (res) => {

      // 2 — Upload directly to Blob using SAS
      await fetch(res.uploadUrl, {
        method: "PUT",
        headers: { "x-ms-blob-type": "BlockBlob" },
        body: this.selectedFile
      });

      alert("Upload Successful!");

      // 3 — Refresh UI
      this.loadFiles();
      this.selectedFile = null;
    });
  }

  loadFiles() {
  this.http.get<UploadedFile[]>(`${this.apiBase}/storage/files`)
    .subscribe(res => {
      this.files = res;
    });
}


  preview(url: string) {
    this.previewUrl = url;
  }
}
