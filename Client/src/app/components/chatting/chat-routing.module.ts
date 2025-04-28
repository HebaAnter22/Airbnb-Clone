import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ChatMainComponent } from './components/chat-main/chat-main.component';
import { ChatConversationComponent } from './components/chat-conversation/chat-conversation.component';
import { ChatWelcomeComponent } from './components/chat-welcome/chat-welcome.component';
import { authGuard } from '../../guards/auth.guard';

const routes: Routes = [
    {
        path: '',
        component: ChatMainComponent,
        canActivate: [authGuard],
        children: [
            {
                path: '',
                component: ChatWelcomeComponent
            },
            {
                path: ':id',
                component: ChatConversationComponent
            }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ChatRoutingModule { }