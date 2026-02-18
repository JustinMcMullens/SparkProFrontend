import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { goalsApi } from '@/lib/api';
import type { CreateGoalRequest } from '@/types';

export function useGoals() {
  return useQuery({
    queryKey: ['goals'],
    queryFn: () => goalsApi.getGoals(),
  });
}

export function useGoal(goalId: number) {
  return useQuery({
    queryKey: ['goals', goalId],
    queryFn: () => goalsApi.getGoal(goalId),
    enabled: !!goalId,
  });
}

export function useMyGoalProgress() {
  return useQuery({
    queryKey: ['goals', 'my-progress'],
    queryFn: () => goalsApi.getMyGoalProgress(),
  });
}

export function useGoalLeaderboard(leaderboardId: number) {
  return useQuery({
    queryKey: ['goals', 'leaderboard', leaderboardId],
    queryFn: () => goalsApi.getGoalLeaderboard(leaderboardId),
    enabled: !!leaderboardId,
  });
}

export function useCreateGoal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateGoalRequest) => goalsApi.createGoal(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['goals'] }),
  });
}

export function useUpdateGoal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      goalId,
      data,
    }: {
      goalId: number;
      data: Partial<CreateGoalRequest>;
    }) => goalsApi.updateGoal(goalId, data),
    onSuccess: (_, { goalId }) => {
      qc.invalidateQueries({ queryKey: ['goals'] });
      qc.invalidateQueries({ queryKey: ['goals', goalId] });
    },
  });
}
