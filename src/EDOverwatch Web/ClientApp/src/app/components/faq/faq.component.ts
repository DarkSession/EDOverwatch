import { Component } from '@angular/core';
import { faArrowUpRightFromSquare } from '@fortawesome/pro-duotone-svg-icons';

@Component({
  selector: 'app-faq',
  templateUrl: './faq.component.html',
  styleUrls: ['./faq.component.css']
})
export class FaqComponent {
  public readonly faArrowUpRightFromSquare = faArrowUpRightFromSquare;
}
