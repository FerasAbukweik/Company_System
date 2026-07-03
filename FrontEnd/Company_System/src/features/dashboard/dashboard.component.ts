import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TopNavComponent } from "../../layout/top-nav/top-nav.component";

interface NavItem {
  icon: string;
  label: string;
  active: boolean;
}

interface ActiveTask {
  id: number;
  title: string;
  category: string;
  priority: 'High' | 'Medium' | 'Low';
  deadline: string;
  status: string;
  borderColorClass: string;
  statusColorClass: string;
}

interface ApprovalRequest {
  id: string;
  name: string;
  role: string;
  avatarUrl: string;
  description: string;
}

interface ActivityItem {
  iconPath: string;
  title: string;
  description: string;
  time: string;
  badgeBgClass: string;
  badgeTextClass: string;
}

interface SystemStat {
  iconPath: string;
  label: string;
  value: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  host:{
    class: 'text-text-primary min-h-screen font-sans p-4 block'
  }
})
export class DashboardComponent {
  // Task Array
  activeTasks = signal<ActiveTask[]>([
    {
      id: 1,
      title: 'Infrastructure Migration Review',
      category: 'Cloud Architecture',
      priority: 'High',
      deadline: 'Today, 5:00 PM',
      status: 'In Progress',
      borderColorClass: 'border-l-secondary-blue',
      statusColorClass: 'bg-secondary-blue/10 text-secondary-blue'
    },
    {
      id: 2,
      title: 'Annual Security Compliance Audit',
      category: 'Compliance',
      priority: 'Medium',
      deadline: 'Oct 24, 2023',
      status: 'Pending Review',
      borderColorClass: 'border-l-outline-strong',
      statusColorClass: 'bg-surface-high text-text-secondary'
    },
    {
      id: 3,
      title: 'Q4 Resource Allocation Sync',
      category: 'Human Resources',
      priority: 'Medium',
      deadline: 'Oct 28, 2023',
      status: 'In Progress',
      borderColorClass: 'border-l-vibrant-turquoise',
      statusColorClass: 'bg-vibrant-turquoise/10 text-corporate-blue'
    }
  ]);

  // Pending Approvals Array
  approvals = signal<ApprovalRequest[]>([
    {
      id: 'Req #992',
      name: 'Elena Voss',
      role: 'DevOps Engineer',
      avatarUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuC3jCA9F60Qt7HAALCzlXvxi0iGHIH-TzpeZvpJbpSEU9Jhly9flTlNFJH4ennWrz5kBXhsV5JZ5k_rMgEOXdjsG31EqKU4H0C3NlCUW38GEVu0v-2pqjtcN7ar-PjKA2IIzLAx_GKCe-46rE-AFPhapVZS7hCib3MAYUr383IB1E27yx5WnN6zXkJe0C9OOsB9-q687o9u-SzwQlwORns4TNV5RXdBJkMVnOeJsh5_txAILI18RCo1KApJ2eY5TLrlf0bIO7r-CeQ',
      description: 'Budget request for additional AWS Aurora instances for the staging environment scalability test.'
    },
    {
      id: 'Req #995',
      name: 'Marcus Thorne',
      role: 'UI/UX Designer',
      avatarUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuD6Qp6PnKobMPZ5Zpc4pLJs5igTnsi4OqKkBkYj-EOLbYrjFlzsRrbnHkYZyzrWt4zJAiSpeM4RAFjLc30-iUNC5w9DCz32lfg31WDfoL2QehJrJ_rjqnnJ6cyZFZeHjasNKnIZy2kHpwKuNrpb5fUmEafJNgBn57erunQtpiMuPEYasSfVge0tm2RdU89qFwNJwGiYjePFU8kGbtKniI69oeazZdC2BcFgiqEjfQpfyFg3_329g-HuzGkVqT6oJ0LubRKGlTy6HAM',
      description: 'Creative Suite license renewal for the design system team (12 seats) for the next fiscal year cycle.'
    }
  ]);

  // Recent Activity Feed Data
  activities = signal<ActivityItem[]>([
    {
      iconPath: 'M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z',
      title: 'System Update:',
      description: 'Automatic backup of the North America node cluster completed successfully.',
      time: '12 minutes ago',
      badgeBgClass: 'bg-vibrant-turquoise/20',
      badgeTextClass: 'text-corporate-blue'
    },
    {
      iconPath: 'M17.982 18.725A7.488 7.488 0 0 0 12 15.75a7.488 7.488 0 0 0-5.982 2.975m11.963 0a9 9 0 1 0-11.963 0m11.963 0A8.966 8.966 0 0 1 12 21a8.966 8.966 0 0 1-5.982-2.275M15 9.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z',
      title: 'Marcus Thorne',
      description: 'moved Design Specs to Final Review.',
      time: '1 hour ago',
      badgeBgClass: 'bg-surface-high',
      badgeTextClass: 'text-text-primary'
    },
    {
      iconPath: 'M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z',
      title: 'Alert:',
      description: 'Unusual traffic spike detected on the API gateway at 02:44 GMT.',
      time: '3 hours ago',
      badgeBgClass: 'bg-error/10',
      badgeTextClass: 'text-error'
    },
    {
      iconPath: 'M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 0 1 .865-.501 48.172 48.172 0 0 0 3.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0 0 12 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018Z',
      title: 'Elena Voss',
      description: 'mentioned you in a comment on the #server-logs channel.',
      time: '5 hours ago',
      badgeBgClass: 'bg-surface-high',
      badgeTextClass: 'text-text-primary'
    }
  ]);



  // Placeholder actions
  onAction(actionType: string, item?: any) {
    console.log(`Action triggered: ${actionType}`, item);
  }
}