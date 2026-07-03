import { Component } from '@angular/core';

@Component({
  selector: 'nav[app-top-nav]',
  imports: [],
  templateUrl: './top-nav.component.html',
  host:{
    class: 'sticky top-0 w-full h-16 bg-surface-base border-b border-outline-light flex justify-between items-center px-8 z-40'
  }
})
export class TopNavComponent {}
