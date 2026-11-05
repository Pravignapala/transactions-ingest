import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

export interface Transaction {
  sourceSystem: string;
  transactionId: number;
  cardNumber?: string;
  locationCode: string;
  productName: string;
  amount: number;
  transactionTimeUtc: string;
  status: string | number;
}

export interface ImportResult {
  batchId: string;
  insertedCount: number;
  updatedCount: number;
  revokedCount: number;
  completedAtUtc: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="font-family: Arial, sans-serif; padding: 24px; max-width: 1100px; margin: auto;">
      <header style="display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #e5e7eb; padding-bottom: 16px; margin-bottom: 20px;">
        <div>
          <h1 style="margin: 0; font-size: 24px; color: #111827;">Retail Payments Transactions Ingest</h1>
          <p style="margin: 4px 0 0 0; color: #6b7280; font-size: 14px;">Consolidated snapshot feed reconciliation platform</p>
        </div>
        <button 
          (click)="triggerManualImport()" 
          [disabled]="isImporting()"
          style="padding: 10px 18px; background-color: #2563eb; color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: bold;">
          {{ isImporting() ? 'Importing Snapshots...' : 'Manual Trigger Import' }}
        </button>
      </header>

      <div *ngIf="lastBatch()" style="background-color: #eff6ff; border: 1px solid #bfdbfe; border-radius: 6px; padding: 12px; margin-bottom: 20px; font-size: 14px;">
        <strong>Last Batch ({{ lastBatch()?.batchId }}):</strong> 
        Inserted: {{ lastBatch()?.insertedCount }} | 
        Updated: {{ lastBatch()?.updatedCount }} | 
        Revoked: {{ lastBatch()?.revokedCount }}
      </div>

      <table style="width: 100%; border-collapse: collapse; text-align: left; font-size: 14px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
        <thead>
          <tr style="background-color: #f3f4f6; border-bottom: 1px solid #d1d5db;">
            <th style="padding: 10px;">Source System</th>
            <th style="padding: 10px;">Tx ID</th>
            <th style="padding: 10px;">Card</th>
            <th style="padding: 10px;">Location</th>
            <th style="padding: 10px;">Product</th>
            <th style="padding: 10px; text-align: right;">Amount</th>
            <th style="padding: 10px;">Transaction Time (UTC)</th>
            <th style="padding: 10px; text-align: center;">Status</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let tx of transactions()" style="border-bottom: 1px solid #e5e7eb;">
            <td style="padding: 10px; font-weight: 500;">{{ tx.sourceSystem }}</td>
            <td style="padding: 10px;">{{ tx.transactionId }}</td>
            <td style="padding: 10px; color: #6b7280;">{{ tx.cardNumber || 'None' }}</td>
            <td style="padding: 10px;">{{ tx.locationCode }}</td>
            <td style="padding: 10px;">{{ tx.productName }}</td>
            <td style="padding: 10px; text-align: right; font-weight: bold;">\${{ tx.amount | number:'1.2-2' }}</td>
            <td style="padding: 10px;">{{ tx.transactionTimeUtc | date:'yyyy-MM-dd HH:mm:ss' }}</td>
            <td style="padding: 10px; text-align: center;">
              <span [style]="getStatusBadgeStyle(tx.status)">
                {{ getStatusLabel(tx.status) }}
              </span>
            </td>
          </tr>
          <tr *ngIf="transactions().length === 0">
            <td colspan="8" style="padding: 24px; text-align: center; color: #6b7280;">
              No transactions imported yet. Click "Manual Trigger Import" to ingest upstream snapshots.
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class AppComponent implements OnInit {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5000/api';

  transactions = signal<Transaction[]>([]);
  isImporting = signal<boolean>(false);
  lastBatch = signal<ImportResult | null>(null);

  ngOnInit(): void {
    this.fetchTransactions();
  }

  fetchTransactions(): void {
    this.http.get<Transaction[]>(`${this.apiUrl}/transactions`).subscribe({
      next: (data) => this.transactions.set(data),
      error: (err) => console.error('Failed to load transactions:', err)
    });
  }

  triggerManualImport(): void {
    this.isImporting.set(true);
    this.http.post<ImportResult>(`${this.apiUrl}/import`, {}).subscribe({
      next: (res) => {
        this.lastBatch.set(res);
        this.isImporting.set(false);
        this.fetchTransactions();
      },
      error: (err) => {
        console.error('Import failed:', err);
        this.isImporting.set(false);
      }
    });
  }

  getStatusLabel(status: any): string {
    const map: Record<string | number, string> = {
      1: 'Active',
      2: 'Updated',
      3: 'Revoked',
      4: 'Finalized',
      'Active': 'Active',
      'Updated': 'Updated',
      'Revoked': 'Revoked',
      'Finalized': 'Finalized'
    };
    return map[status] ?? String(status);
  }

  getStatusBadgeStyle(status: any): string {
    const label = this.getStatusLabel(status);
    const base = 'padding: 4px 8px; border-radius: 12px; font-size: 12px; font-weight: bold; display: inline-block; ';
    switch (label) {
      case 'Active':
        return base + 'background-color: #dcfce7; color: #166534;';
      case 'Updated':
        return base + 'background-color: #fef9c3; color: #854d0e;';
      case 'Revoked':
        return base + 'background-color: #fee2e2; color: #991b1b;';
      case 'Finalized':
        return base + 'background-color: #f3f4f6; color: #374151;';
      default:
        return base + 'background-color: #f3f4f6; color: #1f2937;';
    }
  }
}