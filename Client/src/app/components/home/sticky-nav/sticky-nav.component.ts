import { Component, AfterViewInit, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { ProfileService } from '../../../services/profile.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sticky-nav',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    RouterModule
  ],
  templateUrl: './sticky-nav.component.html',
  styleUrls: ['./sticky-nav.component.css']
})
export class StickyNavComponent implements AfterViewInit {
   lastScrollTop: number = 0;
   footer: HTMLElement | null = null;
   isScrolled: boolean = false; // Added this property
   IsUserGuest:boolean=false;

   @Output() scrollStateChanged = new EventEmitter<boolean>();
    loggedIn: boolean = false;



   constructor(private authService:AuthService,
    private profileService:ProfileService,
    private router:Router
  ) {
    if(this.authService.userId){
      this.loggedIn = true;
    }
  }


   ngAfterViewInit() {
       this.footer = document.getElementById('footer');
       if (this.footer) {
           window.addEventListener('scroll', this.handleScroll.bind(this));
       }
   }

   @HostListener('window:scroll', ['$event'])
   onScroll() {
     const scrolled = window.scrollY > 50;
     
     if (scrolled !== this.isScrolled) {
       this.isScrolled = scrolled;
       this.scrollStateChanged.emit(this.isScrolled);
       
       const header = document.querySelector('.header');
       if (scrolled) {
         header?.classList.add('scrolled');
       } else {
         header?.classList.remove('scrolled');
       }
     }
   }

   handleScroll() {
       const scrollTop: number = window.pageYOffset || document.documentElement.scrollTop;

       if (scrollTop > this.lastScrollTop) {
           // Scrolling down
           this.footer?.classList.add('hidden');
       } else {
           // Scrolling up
           this.footer?.classList.remove('hidden');
       }
       this.lastScrollTop = scrollTop;
   }

   
   editProfileClicked() {
    
    this.router.navigate([`/editProfile/${this.authService.userId}`]);
}
}