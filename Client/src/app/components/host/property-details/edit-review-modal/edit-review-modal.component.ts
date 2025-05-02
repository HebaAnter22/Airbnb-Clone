import { NgForOf, NgIf } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule, NgModel } from '@angular/forms';

@Component({
  selector: 'app-edit-review-modal',
  templateUrl: './edit-review-modal.component.html',
  imports: [FormsModule, NgIf, NgForOf],
  styleUrls: ['./edit-review-modal.component.css']
})
export class EditReviewModalComponent implements OnInit {
  @Input() review: any;
  @Input() showEditReviewModal: boolean = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() saveReview = new EventEmitter<any>();

  editReviewRating: number = 0;
  editReviewComment: string = '';
  reviewId: string = '';

  constructor() { }

  ngOnInit(): void {
    if (this.review) {
      this.initializeReviewData();
    }
  }

  ngOnChanges(): void {
    if (this.review) {
      this.initializeReviewData();
    }
  }

  initializeReviewData(): void {
    this.editReviewRating = this.review.rating;
    this.editReviewComment = this.review.comment;
    this.reviewId = this.review.id;
  }

  closeEditReviewModal(): void {
    this.closeModal.emit();
  }

  setRating(rating: number): void {
    this.editReviewRating = rating;
  }

  submitEditedReview(): void {
    const updatedReview = {
      id: this.reviewId,
      rating: this.editReviewRating,
      comment: this.editReviewComment
    };

    this.saveReview.emit(updatedReview);
  }
}
