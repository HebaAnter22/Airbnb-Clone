import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';

import { ChatMainComponent } from './components/chat-main/chat-main.component';
import { ConversationListComponent } from './components/conversation-list/conversation-list.component';
import { ChatConversationComponent } from './components/chat-conversation/chat-conversation.component';
import { ChatWelcomeComponent } from './components/chat-welcome/chat-welcome.component';
import { ChatRoutingModule } from './chat-routing.module';

@NgModule({

    imports: [
        CommonModule,
        RouterModule,
        ReactiveFormsModule,
        ChatRoutingModule,
        ChatMainComponent,
        ConversationListComponent,
        ChatConversationComponent,
        ChatWelcomeComponent
    ]
})
export class ChatModule { }