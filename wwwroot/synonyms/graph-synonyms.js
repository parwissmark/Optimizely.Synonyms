(() => {
  const { createApp, ref, computed, onMounted } = Vue;

  createApp({
    setup() {
      const baseUrl = `${window.location.origin}/api/plugins/synonyms`;
      const pageTitle = "Synonyms manager";
      const activeTab = ref("synonyms");

      const synonyms = ref([]);
      const logEntries = ref([]);
      const languages = ref([]);

      const availableSlots = ["one", "two"];
      const availableDirections = ["equivalent", "replacement"];

      const searchInput = ref("");
      const filterSlot = ref("");
      const filterLanguage = ref("");
      const filterDirection = ref("");

      const verifySlot = ref("one");
      const verifyLanguage = ref("");
      const verifyLines = ref([]);

      const loading = ref(false);
      const successMessage = ref("");
      const errorMessage = ref("");
      const publishStatusMessage = ref("");
      const publishErrorMessage = ref("");
      const verifyStatusMessage = ref("");
      const verifyErrorMessage = ref("");

      const showFormModal = ref(false);
      const formMode = ref("create");
      const activeSynonym = ref(null);
      const showDeleteModal = ref(false);
      const deleteTarget = ref(null);
      const showLogModal = ref(false);

      const form = ref({
        term: "",
        synonymsText: "",
        synonymSlot: "one",
        languageRouting: "",
        direction: "equivalent",
      });

      const filteredSynonyms = computed(() => {
        const searchTerm = searchInput.value.toLowerCase();
        return synonyms.value.filter((item) => {
          const matchesSearch =
            !searchTerm ||
            item.term.toLowerCase().includes(searchTerm) ||
            item.synonyms.some((synonym) => synonym.toLowerCase().includes(searchTerm));
          const matchesSlot = !filterSlot.value || item.synonymSlot === filterSlot.value;
          const matchesLanguage = !filterLanguage.value || item.languageRouting === filterLanguage.value;
          const matchesDirection = !filterDirection.value || item.direction === filterDirection.value;
          return matchesSearch && matchesSlot && matchesLanguage && matchesDirection;
        });
      });

      const resetMessages = () => {
        successMessage.value = "";
        errorMessage.value = "";
      };

      const resetPublishStatus = () => {
        publishStatusMessage.value = "";
        publishErrorMessage.value = "";
      };

      const resetVerifyStatus = () => {
        verifyStatusMessage.value = "";
        verifyErrorMessage.value = "";
        verifyLines.value = [];
      };

      const request = async (url, options) => {
        const response = await fetch(url, options);
        const data = await response.json();
        if (!response.ok) {
          throw new Error(data?.message || "Request failed");
        }
        return data;
      };

      const loadSynonyms = async () => {
        loading.value = true;
        try {
          const data = await request(`${baseUrl}/synonyms`);
          synonyms.value = data.payload || [];
          resetMessages();
        } catch (err) {
          errorMessage.value = err.message || "Failed loading synonyms.";
        } finally {
          loading.value = false;
        }
      };

      const loadLanguages = async () => {
        try {
          const data = await request(`${baseUrl}/languages`);
          languages.value = data.payload || [];
          if (!verifyLanguage.value && languages.value.length > 0) {
            verifyLanguage.value = languages.value[0];
          }
          if (!form.value.languageRouting && languages.value.length > 0) {
            form.value.languageRouting = languages.value[0];
          }
        } catch (err) {
          console.error(err);
        }
      };

      const openCreateModal = () => {
        formMode.value = "create";
        activeSynonym.value = null;
        form.value = {
          term: "",
          synonymsText: "",
          synonymSlot: "one",
          languageRouting: languages.value[0] || "",
          direction: "equivalent",
        };
        showFormModal.value = true;
      };

      const openEditModal = (item) => {
        formMode.value = "edit";
        activeSynonym.value = item;
        form.value = {
          term: item.term,
          synonymsText: item.synonyms.join(", "),
          synonymSlot: item.synonymSlot,
          languageRouting: item.languageRouting,
          direction: item.direction,
        };
        showFormModal.value = true;
      };

      const parseSynonyms = (text) => {
        if (!text) return [];
        return text
          .split("\n")
          .flatMap((line) => {
            const trimmed = line.trim();
            if (!trimmed) return [];
            if (trimmed.includes("=>")) {
              const parts = trimmed.split("=>");
              return parts[1]?.split(",") || [];
            }
            return trimmed.split(",");
          })
          .map((item) => item.trim())
          .filter((item) => item.length > 0);
      };

      const saveSynonym = async () => {
        resetMessages();
        const payload = {
          term: form.value.term,
          synonyms: parseSynonyms(form.value.synonymsText),
          synonymSlot: form.value.synonymSlot,
          languageRouting: form.value.languageRouting,
          direction: form.value.direction,
        };

        try {
          if (formMode.value === "create") {
            await request(`${baseUrl}/synonyms`, {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify(payload),
            });
            successMessage.value = "Synonym created.";
          } else if (activeSynonym.value) {
            await request(`${baseUrl}/synonyms/${activeSynonym.value.id}`, {
              method: "PUT",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify(payload),
            });
            successMessage.value = "Synonym updated.";
          }

          showFormModal.value = false;
          await loadSynonyms();
        } catch (err) {
          errorMessage.value = err.message || "Failed saving synonym.";
        }
      };

      const publishAll = async () => {
        resetPublishStatus();
        if (synonyms.value.length === 0) {
          publishErrorMessage.value = "No synonyms to publish.";
          return;
        }
        try {
          const data = await request(`${baseUrl}/publish`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ ids: [] }),
          });

          const result = data.payload || {};
          const publishedAt = result.publishedAt ? new Date(result.publishedAt).toLocaleString() : "now";
          publishStatusMessage.value = `${result.message || "Publish succeeded."} ${result.publishedCount || 0} entries published (${publishedAt}).`;
          await loadSynonyms();
        } catch (err) {
          publishErrorMessage.value = err.message || "Failed publishing synonyms.";
        }
      };

      const verifyGraphUpload = async () => {
        resetVerifyStatus();
        try {
          const params = new URLSearchParams({
            synonymSlot: verifySlot.value,
            languageRouting: verifyLanguage.value,
          });
          const data = await request(`${baseUrl}/verify?${params.toString()}`);
          const result = data.payload || {};
          if (result.isSuccess) {
            verifyStatusMessage.value = result.message || "Verification succeeded.";
            verifyLines.value = result.lines || [];
          } else {
            verifyErrorMessage.value = result.message || "Verification failed.";
          }
        } catch (err) {
          verifyErrorMessage.value = err.message || "Verification failed.";
        }
      };

      const openPublishLog = async () => {
        try {
          const data = await request(`${baseUrl}/publish/log`);
          logEntries.value = data.payload || [];
          showLogModal.value = true;
        } catch (err) {
          console.error(err);
        }
      };

      const deleteSynonym = async (id) => {
        if (!id) return;
        try {
          await request(`${baseUrl}/synonyms/${id}`, { method: "DELETE" });
          successMessage.value = "Synonym deleted.";
          await loadSynonyms();
        } catch (err) {
          errorMessage.value = err.message || "Failed deleting synonym.";
        }
      };

      const openDeleteModal = (item) => {
        deleteTarget.value = item;
        showDeleteModal.value = true;
      };

      const closeDeleteModal = () => {
        showDeleteModal.value = false;
        deleteTarget.value = null;
      };

      const confirmDelete = async () => {
        if (!deleteTarget.value) return;
        await deleteSynonym(deleteTarget.value.id);
        closeDeleteModal();
      };

      const clearFilters = () => {
        filterSlot.value = "";
        filterLanguage.value = "";
        filterDirection.value = "";
      };

      const formatDate = (value) => {
        if (!value) return "-";
        return new Date(value).toLocaleString();
      };

      onMounted(() => {
        loadSynonyms();
        loadLanguages();
      });

      return {
        pageTitle,
        activeTab,
        synonyms,
        filteredSynonyms,
        logEntries,
        languages,
        availableSlots,
        availableDirections,
        searchInput,
        filterSlot,
        filterLanguage,
        filterDirection,
        verifySlot,
        verifyLanguage,
        verifyLines,
        loading,
        successMessage,
        errorMessage,
        publishStatusMessage,
        publishErrorMessage,
        verifyStatusMessage,
        verifyErrorMessage,
        showFormModal,
        formMode,
        form,
        showDeleteModal,
        showLogModal,
        openCreateModal,
        openEditModal,
        saveSynonym,
        publishAll,
        verifyGraphUpload,
        openPublishLog,
        clearFilters,
        formatDate,
        openDeleteModal,
        closeDeleteModal,
        confirmDelete,
      };
    },
    template: `
      <div class="ps-panel">
        <h1>{{ pageTitle }}</h1>
        <p class="ps-preamble ps-section-intro">
          Create and manage synonym sets stored in DDS, then publish and verify them in Optimizely Graph.
        </p>

        <div class="ps-tabs">
          <button
            type="button"
            :aria-selected="activeTab === 'synonyms' ? 'true' : 'false'"
            @click="activeTab = 'synonyms'"
          >
            Synonyms
          </button>
          <button
            type="button"
            :aria-selected="activeTab === 'verification' ? 'true' : 'false'"
            @click="activeTab = 'verification'"
          >
            Verification
          </button>
        </div>

        <div v-if="activeTab === 'synonyms'">
          <div class="ps-feedback ps-feedback--info">
            Synonyms are stored in Optimizely DDS and are not available in Graph until you publish.
            Use this tab to manage entries and publish them per slot, language, and direction.
          </div>

          <div class="ps-button-group">
            <button class="ps-button" type="button" @click="openCreateModal">Create synonym</button>
            <button class="ps-button" type="button" @click="publishAll" :disabled="loading || synonyms.length === 0">
              Publish to Graph
            </button>
            <button class="ps-button ps-button--ghost" type="button" @click="openPublishLog">View publish log</button>
          </div>

          <div class="ps-form">
            <div class="ps-field">
              <label class="ps-label" for="synonym-search">Search</label>
              <div class="ps-field-row">
                <input id="synonym-search" class="ps-input" v-model="searchInput" placeholder="Search terms or synonyms" />
                <div class="ps-button-group">
                  <button class="ps-button ps-button--ghost" type="button" @click="searchInput = ''">Clear</button>
                </div>
              </div>
            </div>

            <div class="ps-field">
              <label class="ps-label">Filters</label>
              <div class="ps-field-row">
                <select class="ps-select" v-model="filterSlot">
                  <option value="">All slots</option>
                  <option v-for="slot in availableSlots" :key="slot" :value="slot">{{ slot }}</option>
                </select>
                <select class="ps-select" v-model="filterLanguage">
                  <option value="">All languages</option>
                  <option v-for="language in languages" :key="language" :value="language">{{ language }}</option>
                </select>
                <select class="ps-select" v-model="filterDirection">
                  <option value="">All directions</option>
                  <option v-for="direction in availableDirections" :key="direction" :value="direction">{{ direction }}</option>
                </select>
                <div class="ps-button-group">
                  <button class="ps-button ps-button--ghost" type="button" @click="clearFilters">Clear filters</button>
                </div>
              </div>
            </div>
          </div>

          <div v-if="successMessage" class="ps-feedback ps-feedback--success">{{ successMessage }}</div>
          <div v-if="errorMessage" class="ps-feedback ps-feedback--error">{{ errorMessage }}</div>
          <div v-if="publishStatusMessage" class="ps-feedback ps-feedback--success">{{ publishStatusMessage }}</div>
          <div v-if="publishErrorMessage" class="ps-feedback ps-feedback--error">{{ publishErrorMessage }}</div>

          <div class="ps-table-wrap">
            <table class="ps-table">
              <thead>
                <tr>
                  <th>Term</th>
                  <th class="ps-col-wide">Synonyms</th>
                  <th>Slot</th>
                  <th>Language</th>
                  <th>Direction</th>
                  <th>Updated</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="filteredSynonyms.length === 0">
                  <td colspan="8">No synonyms found.</td>
                </tr>
                <tr v-for="item in filteredSynonyms" :key="item.id">
                  <td>{{ item.term }}</td>
                  <td class="ps-col-wide">{{ item.synonyms.join(', ') }}</td>
                  <td>{{ item.synonymSlot }}</td>
                  <td>{{ item.languageRouting }}</td>
                  <td>{{ item.direction }}</td>
                  <td>{{ formatDate(item.updatedAt) }}</td>
                  <td>{{ item.isPublished ? 'Published' : 'Draft' }}</td>
                  <td>
                    <div class="ps-button-group">
                      <button class="ps-button ps-button--ghost" type="button" @click="openEditModal(item)">Edit</button>
                      <button class="ps-button ps-button--danger" type="button" @click="openDeleteModal(item)">Delete</button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div v-else>
          <div class="ps-feedback ps-feedback--info">
            Verification reads directly from Optimizely Graph and shows what is currently stored there.
            Use it to confirm published data for a specific slot and language.
          </div>

          <div class="ps-form">
            <div class="ps-field">
              <label class="ps-label">Verify upload</label>
              <div class="ps-field-row">
                <select class="ps-select" v-model="verifySlot">
                  <option v-for="slot in availableSlots" :key="slot" :value="slot">{{ slot }}</option>
                </select>
                <select class="ps-select" v-model="verifyLanguage">
                  <option v-for="language in languages" :key="language" :value="language">{{ language }}</option>
                </select>
                <div class="ps-button-group">
                  <button class="ps-button ps-button--ghost" type="button" @click="verifyGraphUpload" :disabled="!verifyLanguage">
                    Verify upload
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div v-if="verifyStatusMessage" class="ps-feedback ps-feedback--success">{{ verifyStatusMessage }}</div>
          <div v-if="verifyErrorMessage" class="ps-feedback ps-feedback--error">{{ verifyErrorMessage }}</div>

          <div v-if="verifyLines.length > 0" class="ps-table-wrap">
            <table class="ps-table">
              <thead>
                <tr><th>Returned entries</th></tr>
              </thead>
              <tbody>
                <tr v-for="(line, index) in verifyLines" :key="index">
                  <td>{{ line }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div v-if="showFormModal" class="ps-modal">
          <div class="ps-modal__content">
            <h3>{{ formMode === 'create' ? 'Create synonym' : 'Edit synonym' }}</h3>
            <div class="ps-form">
              <div class="ps-field">
                <label class="ps-label">Term</label>
                <input class="ps-input" v-model="form.term" placeholder="Term" />
              </div>
              <div class="ps-field">
                <label class="ps-label">Slot, language, direction</label>
                <div class="ps-field-row">
                  <select class="ps-select" v-model="form.synonymSlot">
                    <option v-for="slot in availableSlots" :key="slot" :value="slot">{{ slot }}</option>
                  </select>
                  <select class="ps-select" v-model="form.languageRouting">
                    <option v-for="language in languages" :key="language" :value="language">{{ language }}</option>
                  </select>
                  <select class="ps-select" v-model="form.direction">
                    <option v-for="direction in availableDirections" :key="direction" :value="direction">
                      {{ direction === 'replacement' ? 'replacement (=>)' : 'equivalent (,)' }}
                    </option>
                  </select>
                </div>
              </div>
              <div class="ps-field">
                <label class="ps-label">Synonyms</label>
                <textarea class="ps-textarea" v-model="form.synonymsText" placeholder="Equivalent: laptop, computer, pc\nReplacement: snow => ice, cold"></textarea>
              </div>
              <div class="ps-modal__actions">
                <button class="ps-button ps-button--ghost" type="button" @click="showFormModal = false">Cancel</button>
                <button class="ps-button" type="button" @click="saveSynonym">Save</button>
              </div>
            </div>
          </div>
        </div>

        <div v-if="showDeleteModal" class="ps-modal">
          <div class="ps-modal__content">
            <p>Delete synonym?</p>
            <div class="ps-modal__actions">
              <button class="ps-button ps-button--ghost" type="button" @click="closeDeleteModal">Cancel</button>
              <button class="ps-button ps-button--danger" type="button" @click="confirmDelete">Delete</button>
            </div>
          </div>
        </div>

        <div v-if="showLogModal" class="ps-modal">
          <div class="ps-modal__content">
            <h3>Publish log</h3>
            <div class="ps-table-wrap">
              <table class="ps-table">
                <thead>
                  <tr>
                    <th>When</th>
                    <th>Action</th>
                    <th>Message</th>
                    <th>By</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-if="logEntries.length === 0">
                    <td colspan="4">No publish log entries.</td>
                  </tr>
                  <tr v-for="entry in logEntries" :key="entry.id">
                    <td>{{ formatDate(entry.occurredAt) }}</td>
                    <td>{{ entry.action }}</td>
                    <td>{{ entry.message }}</td>
                    <td>{{ entry.performedBy || '-' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="ps-modal__actions">
              <button class="ps-button ps-button--ghost" type="button" @click="showLogModal = false">Close</button>
            </div>
          </div>
        </div>
      </div>
    `,
  }).mount("#graph-synonyms-app");
})();
