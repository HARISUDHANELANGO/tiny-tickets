import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface UploadedFile {
  id: number;
  fileName: string;
  blobName: string;
  contentType: string;
  size: number;
  uploadedOn: string;
  url: string;
}

interface Ticket {
  id: number;
  title: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './home.html',
  styleUrls: ['./home.scss'],
})
export class HomeComponent {
  private http = inject(HttpClient);
  apiBase = environment.apiUrl;

  // ------------------------------
  // DATA BINDINGS
  // ------------------------------
  tickets: Ticket[] = [];
  files: UploadedFile[] = [];

  title = '';
  selectedFile: File | null = null;

  ngOnInit() {
    this.loadTickets();
    this.loadFiles();
  }

  // ------------------------------
  // TICKETS
  // ------------------------------
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
      .subscribe(() => console.log(`Event published for ticket #${id}`));
  }

  // ------------------------------
  // FILE UPLOAD
  // ------------------------------
  onFileSelected(event: any) {
    const file = event.target.files?.[0];
    if (!file) return;

    this.selectedFile = file;
    this.uploadFile();
  }

  async uploadFile() {
    if (!this.selectedFile) return;

    const guid = crypto.randomUUID();
    const blobName = `${guid}_${this.selectedFile.name}`;
    const container = 'ticket-images';

    // Step 1 — Ask backend for SAS URL
    this.http
      .post<{ uploadUrl: string }>(`${this.apiBase}/storage/sas-upload`, {
        container,
        fileName: blobName,
      })
      .subscribe(async (res) => {
        // Step 2 — Upload to Azure Blob with SAS URL
        await fetch(res.uploadUrl, {
          method: 'PUT',
          headers: { 'x-ms-blob-type': 'BlockBlob' },
          body: this.selectedFile,
        });

        // Step 3 — Save metadata to backend DB
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

  // ------------------------------
  // FILE LIST
  // ------------------------------
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
      .get(`${this.apiBase}/storage/files/report/pdf`, {
        responseType: 'blob',
      })
      .subscribe((blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'TinyTickets_File_Report.pdf';
        a.click();
        URL.revokeObjectURL(url);
      });
  }
}
