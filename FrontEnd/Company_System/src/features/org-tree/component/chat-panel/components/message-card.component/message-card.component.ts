import { Component, input } from '@angular/core';
import { MessageDTO } from '../../../../../../core/dto/message-dto';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-message-card',
  imports: [DatePipe],
  templateUrl: './message-card.component.html',
})
export class MessageCardComponent {
  // input
  message = input.required<MessageDTO>();
}
