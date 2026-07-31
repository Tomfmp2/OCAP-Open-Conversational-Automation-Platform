import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface KnowledgeBase {
 id: string;
 name: string;
 description: string;
 vectorDbProvider: string;
 strategy: string;
 createdAtUtc: string;
 documentCount: number;
 vectorCount: number;
}

export interface KnowledgeJob {
 id: string;
 status: string;
 type: string;
 createdAtUtc: string;
 completedAtUtc?: string;
}

export interface KnowledgeSearchResult {
 documentId: string;
 content: string;
 score: number;
 metadata?: Record<string, unknown>;
}

export interface CreateKnowledgeBasePayload {
 name: string;
 description: string;
 strategy: string;
 vectorDbProvider: string;
}

export function useKnowledgeData() {
 const queryClient = useQueryClient();

 const listQuery = useQuery<KnowledgeBase[]>({
 queryKey: ["knowledgeBases"],
 queryFn: async () => {
 const data = await apiClient.get<
 Array<{
 id: string;
 name: string;
 description: string;
 vectorDbProvider: string;
 strategy: string;
 createdAtUtc: string;
 documentCount: number;
 vectorCount: number;
 }>
 >("/api/knowledge");

 return data.map((kb) => ({
 id: kb.id,
 name: kb.name,
 description: kb.description,
 vectorDbProvider: kb.vectorDbProvider,
 strategy: kb.strategy,
 createdAtUtc: kb.createdAtUtc,
 documentCount: kb.documentCount ?? 0,
 vectorCount: kb.vectorCount ?? 0,
 }));
 },
 staleTime: 10000,
 });

 const jobsQuery = useQuery<KnowledgeJob[]>({
 queryKey: ["knowledgeJobs"],
 queryFn: async () => {
 const data = await apiClient.get<KnowledgeJob[]>("/api/knowledge/jobs");
 return Array.isArray(data) ? data : [];
 },
 staleTime: 10000,
 });

 const statusQuery = useQuery({
 queryKey: ["knowledgeStatus"],
 queryFn: () => apiClient.get("/api/knowledge/status"),
 staleTime: 30000,
 });

 const createMutation = useMutation({
 mutationFn: (payload: CreateKnowledgeBasePayload) =>
 apiClient.post("/api/knowledge", payload),
 onSuccess: () => {
 queryClient.invalidateQueries({ queryKey: ["knowledgeBases"] });
 },
 });

 const deleteDocumentMutation = useMutation({
 mutationFn: (documentId: string) => apiClient.delete(`/api/knowledge/${documentId}`),
 onSuccess: () => {
 queryClient.invalidateQueries({ queryKey: ["knowledgeBases"] });
 queryClient.invalidateQueries({ queryKey: ["knowledgeJobs"] });
 },
 });

 const uploadMutation = useMutation({
 mutationFn: ({
 file,
 knowledgeBaseId,
 category,
 }: {
 file: File;
 knowledgeBaseId: string;
 category: string;
 }) => {
 const formData = new FormData();
 formData.append("file", file);
 formData.append("knowledgeBaseId", knowledgeBaseId);
 formData.append("category", category);
 return apiClient.upload("/api/knowledge/upload", formData);
 },
 onSuccess: () => {
 queryClient.invalidateQueries({ queryKey: ["knowledgeBases"] });
 queryClient.invalidateQueries({ queryKey: ["knowledgeJobs"] });
 },
 });

 const searchMutation = useMutation({
 mutationFn: ({
 query,
 strategy = "Hybrid",
 topK = 5,
 }: {
 query: string;
 strategy?: string;
 topK?: number;
 }) =>
 apiClient.get<KnowledgeSearchResult[]>(
 `/api/knowledge/search?query=${encodeURIComponent(query)}&strategy=${strategy}&topK=${topK}`
 ),
 });

 const reindexMutation = useMutation({
 mutationFn: (knowledgeBaseId: string) =>
 apiClient.post("/api/knowledge/reindex", { knowledgeBaseId }),
 onSuccess: () => {
 queryClient.invalidateQueries({ queryKey: ["knowledgeBases"] });
 queryClient.invalidateQueries({ queryKey: ["knowledgeJobs"] });
 },
 });

 return {
 ...listQuery,
 jobs: jobsQuery.data ?? [],
 status: statusQuery.data,
 createMutation,
 deleteDocumentMutation,
 uploadMutation,
 searchMutation,
 reindexMutation,
 refetchJobs: jobsQuery.refetch,
 };
}
