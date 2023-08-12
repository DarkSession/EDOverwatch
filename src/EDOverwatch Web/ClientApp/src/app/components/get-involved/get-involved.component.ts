import { Component } from '@angular/core';
import { faArrowUpRightFromSquare } from '@fortawesome/pro-duotone-svg-icons';

@Component({
  selector: 'app-get-involved',
  templateUrl: './get-involved.component.html',
  styleUrls: ['./get-involved.component.scss']
})
export class GetInvolvedComponent {
  public readonly faArrowUpRightFromSquare = faArrowUpRightFromSquare;
}

